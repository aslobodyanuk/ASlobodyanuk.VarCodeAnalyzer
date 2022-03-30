# ASlobodyanuk.VarCodeAnalyzer

This visual studio extension adds a new diagnostic and fix for 'var' type declaration misuses with special rules.

<img src="https://user-images.githubusercontent.com/27679130/160798712-1c9a9cb2-af68-49e2-991c-cea3a0ce108e.png" width="300">

In this case the variable's type is not obvious:
```csharp
var shouldReload = list.Take(1);
```

After code fix variable declaraiton would look like:
```csharp
IEnumerable<string> shouldReload = list.Take(1);
```

## Solution FixAllProvider 
There is also a solution wide fixer available.

<img src="https://user-images.githubusercontent.com/27679130/160800100-ec780e91-8551-4859-a093-4af615b4f892.png" width="500">

Which would diagnose all 'var' issues throught the entire solution and present a list of fixes for them.

<img src="https://user-images.githubusercontent.com/27679130/160800557-fc2a2d5e-fbe1-4974-b3ee-09fcc13a2327.png" width="500">

Supported fixer scopes can be determined via `GetSupportedFixAllScopes` method result list.

Available scopes:
- FixAllScope.Solution
- FixAllScope.Project
- FixAllScope.Document

<img src="https://user-images.githubusercontent.com/27679130/160805876-e3f7fc64-8a9e-46ac-8962-1a5c8b18f101.png" width="500">

## Rules

There are only exclusion rules, meaning that provided cases below will be excluded, otherwise a 'var' declaration will be replaced with an actual type.
Please follow this example on how 'ShouldBeExplicit' method works:

 `ShouldBeExplicit(string variableDeclaration, string typeDeclaration)`

variableDeclaration: `var shouldReload = list.Take(1);`

typeDeclaration: `IEnumerable<string>`

```csharp
variableDeclaration.StartsWith(typeDeclaration)
variableDeclaration.Contains($"<{typeDeclaration}>")
variableDeclaration.Contains($"new {typeDeclaration}")
variableDeclaration.Contains($" ({typeDeclaration})")
typeDeclaration.Contains("<anonymous type:")
variableDeclaration.Contains($"as {typeDeclaration}")
```

## Code Flow

This section explains how a code diagnostic extension and fixer are working inside Visual Studio.

### Single line fix

1. Visual Studio loads the extension and scans open documents with `VariableCodeAnalyzer`
2. Diagnostic information will be displayed in Error List window. (with Information severity)
3. You press Alt + Enter or hover over a suggestion in Visual Studio
4. Diagnostic object is retrieved from `CodeFixContext context`
5. `VariableCodeFixProvider` code fix executed, that is registered in `RegisterCodeFixesAsync` method
6. `ASCodeFixService.ApplyDiagnosticFixAsync` is called in order to provide a code change (returns whole content of .cs with changed variable declaration)
7. You click on the solution or press enter in a popup window
8. Variable declaration is replaced with an actual type

### Whole solution fix

If you selected 'Solution' to fix all diagnosed issues throught the solution:

<img src="https://user-images.githubusercontent.com/27679130/160800100-ec780e91-8551-4859-a093-4af615b4f892.png" width="500">

1. Whole solution will be scanned using `VariableCodeAnalyzer`
2. For each problem there will be an instance of Diagnostic created
3. `ASVariableFixAllProvider` code fix executed, that is registered in `GetFixAsync` method
4. For each document (csharp file) in the solution a code fixer will be executed `ASCodeFixService.ApplyDiagnosticFixesAsync`
5. Code fixer will calculate required changes to the documents, based on the diagnostics list
6. A modified solution instance will be created as a result of `ChangeSolution` method execution (with modified documents)
7. A window with a list of changes will be presented to you

## Syntax Visualizer

There is a helpful tool, that allows you to see the syntax tree objects live.

Use it in order to figure out the code structure that needs changes.

You can find it in Visual Studio: View -> Syntax Visualizer

<img src="https://user-images.githubusercontent.com/27679130/160807511-b049ae2e-f24c-481c-961d-3d954cb680b4.png" width="500">

## Search tags:
- Visual Studio custom extension
- FixAllProvider example
- Custom FixAllProvider
- Custom code cleanup fixer
- Custom code fix extension
- Code diagnostic
- Code analyzer
- C#

