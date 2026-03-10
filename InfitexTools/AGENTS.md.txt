# InfitexTools

Project type:
SolidWorks add-in written in C# on .NET Framework.

Goal:
Build a lightweight PDM-like tool for one engineer, focused on:
- Task Pane navigation
- custom properties editing
- drawing revision workflow
- part numbering
- Excel synchronization

Main technologies:
- SolidWorks COM API
- WinForms
- Excel as current data source
- Visual Studio solution

Important files:
- Connect.cs -> add-in entry point
- InfitexTaskPaneControl.cs -> task pane UI
- PropertiesService.cs -> read/write custom properties
- PropertiesFormV2.cs -> properties editor
- Models/* -> DTOs for model and drawing data

Current priorities:
1. Finish Properties v2 for PART/ASM
2. Then implement DRW mode
3. Then Excel-backed project lists and user lists
4. Then Assign Index
5. Then Clone as New Number
6. Then revision workflow

Rules:
- Prefer minimal, practical solutions over overengineering
- Keep UI simple and fast
- Do not replace Excel with SQLite unless explicitly requested
- Do not rewrite working Task Pane code unless necessary
- Keep SolidWorks API calls isolated in services where possible

Naming conventions:
- Index
- ActualRevision
- FileType
- CreatedBy
- Description_EN
- Description_PL
- PartType
- Supplier
- ProjectNumber
- ProjectName
- Remark
- DownloadLink
- Comments

PART/ASM Properties v2:
Tabs:
- Main
- Comments

DRW Properties v2:
Tabs:
- Drawing
- History

Behavior notes:
- For drawings, Description_EN/PL and ProjectNumber/Name are pulled from referenced model and propagated to drawing
- Revision field in drawing is read-only
- ChangeStatus values are only: Approved, In Work
- Users list comes from Excel
- Revision history is hybrid and based on previous macro logic

What to avoid:
- Do not introduce unnecessary abstractions
- Do not move to WPF
- Do not build a custom tree-grid unless explicitly requested