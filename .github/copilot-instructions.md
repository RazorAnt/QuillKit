# General Instructions for GitHub Copilot

## Additional Instructions
**MANDATORY: At the start of EVERY session, you MUST check for `.github/personal-instructions.md` using BOTH of the following methods, regardless of previous results:**
  1. Use file_search for `.github/personal-instructions.md`.
  2. Use list_dir on the `.github/` directory to verify its presence.

**You must perform BOTH steps every time, even if the file is not found in the first step. Do not proceed until BOTH checks are complete.**

  - **ALWAYS read the entire personal-instructions.md file** before proceeding with any tasks, regardless of other context or attachments.
  - **Personal instructions ALWAYS take precedence** over general team instructions when there are conflicts.
  - If the personal instructions file does not exist, continue with the general team instructions - not everyone will have personal instructions.

## Boundaries and Constraints
- CRITICAL: Only make changes explicitly requested by me. Do not perform additional tasks.
- Do not attempt to "fix" or "clean up" code unless specifically instructed.
- Always confirm changes before making them. Ask "Should I make this change?" if there's any ambiguity.
- If a task seems complex, break it down and ask for confirmation at each step.
- When I ask for a specific change, make ONLY that change - do not add "improvements" or "optimizations."
- Do not refactor code unless I specifically request it.
- Suggest changes to errors or for refactoring but WAIT for explicit approval before executing any suggested changes.
- If you can not do a task or have a limitation, tell me that instead of guessing.

## Code Editing Guidelines (CRITICAL)

### Making Changes
- CRITICAL: Only make changes explicitly requested. Do not perform additional tasks.
- When modifying a line: If the instruction is "change variable x to y", ONLY change "x" to "y" and nothing else
- If you must make multiple edits, make them individually to avoid unintended formatting changes
- Do not combine, split, or rearrange lines of code unless specifically requested
- Do not fix code that you are not directed to touch even if you believe it is an error. Ask for confirmation.
- Ask for clarification if unsure about what changes are needed rather than making assumptions

### Formatting Preservation (CRITICAL)
- CRITICAL: NEVER modify ANY whitespace, line breaks, indentation, or formatting that is not EXPLICITLY part of the requested change
- When replacing text, copy the EXACT formatting and only change the specific characters requested
- When using replace_string_in_file, the replacement should have IDENTICAL spacing, indentation, and line breaks as the original
- For whitespace-sensitive languages, treat ALL whitespace as significant and preserve it exactly
- If your change does not change any logic, don't make it. You are just messing up formatting.

### Post-Edit Verification (CRITICAL)
- CRITICAL: After making ANY changes, carefully examine the changes for formatting inconsistencies
- Verify proper indentation, spacing, and line breaks throughout the file
- Pay special attention to:
  - Spacing between closing and opening braces
  - Proper indentation at the start of lines
  - Consistent spacing around keywords and operators
  - Line breaks between logical code blocks
- If unsure about formatting, ask before making changes
- If unsure about a particular formatting convention, ask for clarification

## Code Style
- Every public method should have a simple comment at the top inside the method that explains what it does, including an emoji
- Private methods don't need comments unless they're complex
- Use meaningful variable and method names that clearly express intent
- Use PascalCase for classes, methods, and properties
- Use camelCase for local variables and parameters
- Use 4-space indentation
- Prefer async/await over traditional callbacks or Task continuations

## Documentation Standards
- Use XML comments for public APIs and interfaces
- Include parameter descriptions and return value information
- Keep inline comments focused on "why" not "what" (the code should be clear enough to show what it's doing)

## Error Handling
- Use try/catch blocks for recoverable errors
- Always log exceptions with appropriate context
- Consider using custom exception types for domain-specific errors
- Validate inputs at the boundaries of your application

## Database Patterns
- Use repository pattern for data access when possible
- Keep SQL queries in stored procedures when appropriate
- Use parameters to prevent SQL injection
- Implement proper transaction handling

## Templates

