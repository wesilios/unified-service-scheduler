# Skill: Clean Code — Naming & Function Clarity

## Objective

A name is a promise: it tells the reader what a thing *is* or what a function *does*, without
them needing to read the body. Every name that breaks that promise costs every future reader —
including an AI agent working from the name alone — the time it takes to discover the gap
between what the name claims and what the code actually does. This skill exists to catch that
class of bug during review, not just during initial writing.

This is about **naming and signature honesty**, not formatting. It doesn't cover indentation,
brace style, or file layout — those are enforced by `.editorconfig`/the compiler. It covers the
one thing tooling can't check: does the name match reality.

---

## 1. A variable's name must match what it actually holds, not what it's near

The most common failure mode isn't a bad name in isolation — it's a name that was accurate for
an earlier version of the code and silently went stale as the code around it changed.

**Real example caught in this codebase:**

```csharp
// Before
var dealershipService = configuration.GetSection("InfrastructureClients:DealershipService:Http:BaseUrl").Value;
if (!string.IsNullOrEmpty(dealershipService))
{
    services.AddRefitClient<IDealershipHttpClient>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(dealershipService));
}
```

`dealershipService` reads like it holds a service instance or a client. It actually holds a
**config string** — the base URL, nothing else. A reader skimming this sees `dealershipService`
and reasonably assumes something service-shaped; the truth (`string?`, possibly empty) only
shows up if they read the `GetSection(...).Value` call closely. Fix: name it for what it is,
including its role, not just its topic:

```csharp
var dealershipServiceBaseUrl = configuration.GetSection("InfrastructureClients:DealershipService:Http:BaseUrl").Value;
```

**Rule of thumb**: if you'd need to hover the variable in an IDE to know its type, the name
hasn't done its job. `dealershipServiceBaseUrl` doesn't need a hover — the type and the role are
both in the name.

## 2. A function's name must not shadow (misrepresent) its actual behavior

"Shadowing" here doesn't mean the C# scoping term (a local hiding an outer variable — see §5 for
that). It means a name that *hides the true behavior behind a narrower or different-sounding
one* — the reader takes the name at face value and is wrong about what happens when it's called.

**Real example caught in this codebase:**

```csharp
// Before
private static void AddHttpServices(this IServiceCollection services, IConfiguration configuration)
{
    var dealershipServiceBaseUrl = ...;
    if (!string.IsNullOrEmpty(dealershipServiceBaseUrl))
    {
        services.AddTransient<IDealershipProvider, DealershipProvider>();   // real, HTTP-backed
        services.AddRefitClient<IDealershipHttpClient>()...
    }
    else
    {
        services.AddSingleton<IDealershipProvider, MockDealershipProvider>();  // NOT http at all
    }
    // ...same pattern for Technician, ServiceBay
}
```

`AddHttpServices` promises it registers HTTP-based services. Half the time — whenever a
`BaseUrl` isn't configured — it registers an in-memory mock with no HTTP involved at all. A
caller reading only the name would reasonably assume every code path here ends in a real network
client; one of the two branches directly contradicts that. Fix: name it for the actual contract
(registers the provider abstractions, real-or-mock, config-driven), not for the branch that
happens to be more interesting:

```csharp
private static void AddInternalServiceProviders(this IServiceCollection services, IConfiguration configuration)
```

**Rule of thumb**: read the name in isolation, then read every branch of the function body. If
any branch does something the name didn't prepare you for, the name is wrong — fix the name, not
just your mental model of the exception.

## 3. Boolean names must read as a yes/no question, and match the branch that follows

`technicianExists`, `serviceBayExists`, `validation.IsValid` — all good in this codebase: each
reads as a question, and the `if`/`if not` that follows answers it directly. Avoid `flag`,
`check`, `status` as a boolean's name — none of those are yes/no questions on their own.

## 4. No abbreviation the reader has to decode

`serviceTypeCode`, `dealershipId`, `cancellationToken` — spelled out, no ambiguity. Avoid `svc`,
`dlrshp`, `ct`, `req`/`res` where `service`/`dealership`/`cancellationToken`/`request`/`response`
cost nothing extra to type and remove a lookup. The one broadly-accepted exception in this
codebase: short LINQ/lambda parameter names (`x`, `c`, `a`) where the type is obvious from
context one token away (`RuleFor(x => x.Vehicle)`, `customer.Property(c => c.Name)`) — don't
rename those, expanding them adds noise without adding clarity.

## 5. Real C# variable shadowing — a compiler-checkable case, still worth a deliberate look

Unlike §2's "misleading name" sense, this is the literal language feature: an inner scope
declaring a variable with the same name as one in an outer scope, so the inner one silently wins
for the rest of that scope. The compiler warns (`CS0136`) when this actually hides an *active*
outer local — treat that warning as a build-breaking issue, not a suggestion. It does **not**
warn when two same-named parameters belong to two unrelated methods (e.g. an extension method
`Foo(this IServiceCollection services, ...)` called from another method that also has a
`services` local) — that's normal, idiomatic parameter naming, not shadowing, and doesn't need
renaming just because the token repeats.

## 6. Reviewing an existing codebase for this class of issue

1. Read every public method's signature in isolation — name, parameters, return type — before
   reading its body. Write down what you expect it to do.
2. Read the body. Note every place reality diverges from what you wrote down in step 1.
3. For each divergence, decide: is the *name* wrong (§1/§2), or is the *behavior* wrong (a
   correctness bug — out of this skill's scope, flag separately)?
4. Fix the name to match the behavior — never the reverse, unless the behavior is also being
   changed for an independent reason.
5. Re-run the build and full test suite after a rename — a rename should never change behavior,
   and running the suite is how you prove that claim rather than assume it.

## 7. Checklist

- [ ] Every variable name reflects its actual type/role, not just its topic (§1)
- [ ] Every function name covers every branch of its own body, not just the common case (§2)
- [ ] Every boolean reads as a yes/no question (§3)
- [ ] No abbreviation forces a lookup, except conventional short lambda parameters (§4)
- [ ] No compiler shadowing warning (`CS0136`) left unaddressed (§5)
- [ ] Full build + test run after any rename, to confirm behavior is unchanged (§6)
