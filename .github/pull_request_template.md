## Description
<!-- Brief summary of what this PR does and why the change is needed. -->

## Related Issue
- Fixes #<issue-number>

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Code refactor
- [ ] Breaking change
- [ ] Documentation update

## Area
- [ ] Ingestion
- [ ] Feature engineering
- [ ] Model training
- [ ] Championship forecast
- [ ] Frontend
- [ ] Other: <!-- specify -->

---

<!-- Fill in the section that matches your Type of Change, then delete the other one. -->

## Bug Fix Details
<!-- Delete this section if this PR is not a bug fix. -->

**What was happening?**
<!-- What you did, what you expected, and what actually happened. -->

**Steps to reproduce (before this fix)**
1. <!-- e.g. Ingest season 2024 -->
2. <!-- e.g. Rebuild features -->
3. ...

**Relevant logs or output (before this fix)**
```shell
<!-- Paste logs or error output here -->
```

## Feature Details
<!-- Delete this section if this PR is not a new feature. -->

**What problem does this solve?**
<!-- The motivation: a use case, a limitation you hit, a gap in the model. -->

**Proposed solution**
<!-- How this PR solves it. -->

**Alternatives considered**
<!-- Other approaches you weighed and why you didn't choose them. -->

**Additional context**
<!-- Links, references (e.g. an OpenF1 endpoint), or prior art. -->

---

## How Has This Been Tested?
- Describe the tests you ran.
- Provide instructions so reviewers can verify.

**Environment**
- OS:
- .NET SDK version:
- Node version:
- Database (local Postgres / Neon / other):

## Checklist
- [ ] My code follows the conventions in `.claude/rules/conventions.md`
- [ ] `dotnet build F1Predictor.slnx` passes (warnings are errors in this repo)
- [ ] I have performed a self-review of my own code
- [ ] I have added or updated tests where it makes sense
- [ ] Documentation updated where behaviour changed