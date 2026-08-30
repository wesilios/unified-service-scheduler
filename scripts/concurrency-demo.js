#!/usr/bin/env node
// Fires REQUEST_COUNT genuinely concurrent booking requests at the exact same
// Technician/Service Bay/time slot and reports the status-code breakdown.
//
// This is the project's core requirement (concurrency safety), demonstrated by hand: expect
// exactly one 201 Created and the rest 409 Conflict, proving the database never double-books
// the slot even when requests race. It's a convenient way to SEE the guarantee — the actual,
// CI-verified proof is `CreateAppointment_ConcurrentRequestsForSameSlot_ExactlyOneSucceeds`
// in Scheduler.IntegrationTests (see README.md's "Demonstrating the concurrency guarantee"
// section), which drives the same kind of concurrent dispatch straight into an in-process
// TestServer under xUnit, deterministically, on every PR.
//
// Usage:
//   dotnet run --project src/Scheduler.Api                 (default "http" launch profile, :5207)
//   node scripts/concurrency-demo.js
//
// Optional: point at a different instance (e.g. the "https" launch profile) with BASE_URL:
//   BASE_URL=https://localhost:7048 NODE_TLS_REJECT_UNAUTHORIZED=0 node scripts/concurrency-demo.js
// (NODE_TLS_REJECT_UNAUTHORIZED=0 is needed against :7048 only because the dev-mode HTTPS
// certificate is self-signed — Node's fetch doesn't have a simpler per-request "ignore this
// self-signed cert" option. Not needed for the default http://localhost:5207.)

const BASE_URL = process.env.BASE_URL ?? "http://localhost:5207";
const REQUEST_COUNT = Number(process.env.REQUEST_COUNT ?? 20);

// "Next business day, clamped into 08:00-16:00 operating hours, skip Sunday" — a fixed,
// always-valid time rather than a literal date that eventually lands in the past.
// Deliberately dealership-local wall-clock time, not UTC: operating hours are a local-time
// concept (see AppointmentSchedulingPolicy in the Domain layer).
function businessDayIn(offsetDays) {
  const pad = (n) => String(n).padStart(2, "0");
  const now = new Date();
  const hour = Math.min(Math.max(now.getHours(), 8), 16);
  const minute = now.getMinutes();
  const d = new Date(now.getFullYear(), now.getMonth(), now.getDate() + offsetDays);
  if (d.getDay() === 0) {
    d.setDate(d.getDate() + 1); // dealership is closed Sunday - roll forward to Monday
  }
  d.setHours(hour, minute, 0, 0);
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:00`;
}

const startTime = businessDayIn(1);

// A fresh random Technician/Service Bay pair every run (any non-empty GUID is accepted by
// the mocked Technician/Service Bay validation — see README's "Service types you can book"
// section) means this run can never collide with a slot booked by an earlier run, no matter
// how recently. Reusing a fixed pair would collide with your own last run for up to the
// booked service's full duration (a 30-minute OIL_CHANGE occupies two 15-minute slots, so
// even a start time a minute or two later than last time still overlaps it) - that's the
// double-booking guarantee correctly rejecting you, not a bug, but it's an annoying way to
// discover it interactively.
const technicianId = crypto.randomUUID();
const serviceBayId = crypto.randomUUID();

async function main() {
  console.log(`Booking ${REQUEST_COUNT} concurrent requests for the same slot`);
  console.log(`  ${BASE_URL}/appointments`);
  console.log(`  startTime: ${startTime}`);
  console.log();

  const requests = Array.from({ length: REQUEST_COUNT }, (_, i) =>
    fetch(`${BASE_URL}/appointments`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        customerName: `Concurrency Demo ${i + 1}`,
        customerEmail: `concurrency-demo-${i + 1}@example.com`,
        customerPhone: `+63917000${String(i + 1).padStart(4, "0")}`,
        vehicle: "Toyota - Vios - Vios G 2019",
        serviceTypeCode: "OIL_CHANGE",
        dealershipId: "11111111-1111-1111-1111-111111111111",
        technicianId,
        serviceBayId,
        startTime,
      }),
    }).then(async (response) => ({
      request: i + 1,
      status: response.status,
      body: await response.text(),
    })),
  );

  const results = await Promise.all(requests);

  console.table(results.map(({ request, status }) => ({ request, status })));

  const created = results.filter((r) => r.status === 201);
  const conflicted = results.filter((r) => r.status === 409);
  const unexpected = results.filter((r) => r.status !== 201 && r.status !== 409);

  console.log();
  if (created.length === 1 && conflicted.length === REQUEST_COUNT - 1 && unexpected.length === 0) {
    console.log(
      `PASS: exactly 1 request got 201 Created (request #${created[0].request}), the other ${conflicted.length} got 409 Conflict.`,
    );
    process.exitCode = 0;
  } else {
    console.log(
      `FAIL: expected exactly 1x 201 and ${REQUEST_COUNT - 1}x 409 - got ${created.length}x 201, ${conflicted.length}x 409, ${unexpected.length} unexpected status(es).`,
    );
    if (unexpected.length > 0) {
      console.log("Unexpected responses:", unexpected);
    }
    process.exitCode = 1;
  }
}

main().catch((err) => {
  console.error(`Could not reach ${BASE_URL} - is the API running? (dotnet run --project src/Scheduler.Api)`);
  console.error(err.message);
  process.exitCode = 1;
});
