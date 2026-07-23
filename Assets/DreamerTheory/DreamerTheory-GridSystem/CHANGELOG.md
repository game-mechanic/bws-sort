# Changelog

All notable changes to the **DreamerTheory-GridSystem** project will be documented in this file.

## [1.1.0] - 2026-06-05
### Fixed
- **Build Compatibility**: Moved `#if UNITY_EDITOR` preprocessor wrappers inside virtual method bodies (`OnDrawGizmos()`, `OnValidate()`, and `ResizeGridIfNeededEditor()`) rather than wrapping the entire methods. This keeps virtual function signatures intact on non-editor platforms to avoid compiler errors.

## [1.0.2] - 2026-05-19
### Added
- **Multi-Plane Support for Hex Grids**: Added XY plane support to the hexagonal grid system, allowing hexagonal layouts in both 2D (XY) and 3D (XZ) space.
### Refactored
- **HexGridSystem Consolidation**: Renamed `HexGridSystem3d.cs` to `HexGridSystem.cs` to handle both XY and XZ layouts in a single unified script.

## [1.0.1] - 2026-05-18
### Added
- **Separate Cell Size dimensions**: Replaced single-float cell size with vector dimensions, allowing separate width and height configurations for grid cells.
### Fixed
- **Cell Indexing**: Corrected grid coordinate and index conversion/handles under varying cell dimensions.

## [1.0.0] - 2026-04-22
### Added
- **Safety Guards**: Introduced a private `_isResizing` guard flag in the base `GridSystem` to prevent concurrent read/write operations during grid reallocation, preventing `IndexOutOfRangeException`.
- **Try-Finally in Editor Cycles**: Wrapped editor resizing in `OnValidate` using try-finally block to guarantee safety flags are always cleaned up.
- **Auto-Initialization**: Configured automatic `gridArray` initialization on script load in editor (when `previousGridSize == Vector2Int.zero`), with Undo registration and marking dirty.

## [0.2.0] - 2025-12-28
### Added
- **Grid Resizing**: Added `ResizeGridIfNeededEditor()` and other helper methods in the base `GridSystem` class to support dynamic resizing in the editor.
- **Custom Attributes & Drawers**: Created `GridSizeDrawer.cs` and `DelayedGridSize.cs` to delay and handle grid sizing inputs smoothly in the Unity inspector.
- **Documentation**: Bundled `GridSystem.pdf` documentation.

## [0.1.0] - 2025-12-27
### Added
- **Initial Release**: Core generic grid components (`GridSystem<T>`, `GridSystem2D<T>`, and `GridSystem3D<T>`).
