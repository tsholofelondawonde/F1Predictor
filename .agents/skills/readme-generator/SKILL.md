---
name: readme-generator
description: Use when asked to generate or refresh a project's README.md — gathers context from whatever manifests, docs, and AI-agent instruction files (CLAUDE.md, AGENTS.md, .cursorrules, copilot-instructions.md, etc.) actually exist in the repo.
---

# README Generator

Generate a comprehensive README.md for this repository. Source material
varies by project — gather from whatever actually exists below and skip
what doesn't, rather than assuming a fixed set of files.

1. Gather source material:
   - **Existing README.md**, if present — preserve anything still accurate.
   - **Package/build manifests** (`package.json`, `pyproject.toml`,
     `Cargo.toml`, `go.mod`, `pom.xml`, `*.csproj`, etc.) for the project
     name, dependencies, and available scripts/commands.
   - **AI-agent instruction files**, wherever the project keeps them —
     `CLAUDE.md`, `AGENTS.md`, `.github/copilot-instructions.md`,
     `.github/copilot/` docs, `.cursorrules`, `.windsurfrules`, or
     equivalent — for architecture notes, conventions, and workflow
     already written down for coding agents.
   - **`docs/`, ADRs, or diagrams** for architecture and design decisions.
   - **CI config** (`.github/workflows/`, `.gitlab-ci.yml`,
     `azure-pipelines.yml`, etc.) for build/test/deploy workflow and
     branching conventions.
   - **Test directories and config** for the testing approach and
     tooling in use.
   - **CONTRIBUTING.md and LICENSE**, if present.
   - **The source tree itself** (top-level folders, entry points) when no
     structure docs exist — infer organization directly from the code.

2. Create README.md with the following sections, populated from whatever
   sources above actually covered that topic:

## Project Name and Description
- Extract the project name and primary purpose.
- Include a concise description of what the project does.

## Technology Stack
- List the primary languages, frameworks, and key dependencies.
- Include version information when available (manifest/lockfile).

## Project Architecture
- Provide a high-level overview of the architecture.
- Include a simple diagram if one exists in the docs, or a short textual
  summary of major components/services and how they interact.

## Getting Started
- Installation instructions based on the detected tech stack.
- Setup/configuration steps and prerequisites.

## Project Structure
- Brief overview of the folder organization, sourced from docs or
  inferred from the top-level source tree.

## Key Features
- Main functionality and features, drawn from docs, code, and any
  existing README content.

## Development Workflow
- Summarize the development process (build/test/run commands, branching
  strategy) sourced from CI config and package manifests.

## Coding Standards
- Summarize key conventions, sourced from linter/formatter config,
  AI-agent instruction files, or CONTRIBUTING.md.

## Testing
- Explain the testing approach and tooling, sourced from test config and
  directory structure.

## Contributing
- Guidelines for contributing, sourced from CONTRIBUTING.md and any
  agent instruction files.

## License
- Include license information if a LICENSE file is present.

3. Format the README with proper Markdown:
   - Clear headings and subheadings.
   - Code blocks for commands and config.
   - Lists for scannability.
   - Links to other docs in the repo.
   - Badges for build status/version if that information is available.

Keep the README concise yet informative, focusing on what a new
contributor or user would actually need to get productive.
