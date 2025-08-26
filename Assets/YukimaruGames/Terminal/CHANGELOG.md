# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2025-08-26

### Fixed
- Fixed a rendering lag issue with the input field that occurred during re-focus when a uGUI Canvas was also present in the scene. This was resolved by separating the input field from the log's `ScrollViewScope` to prevent IMGUI event conflicts.

## [1.0.0] - 2025-08-21

### Changed
- This is a complete redesign and refactor based on a previous internal alpha project, rebuilt from the ground up on Clean Architecture principles with new features like dependency separation and dynamic delegate generation.


### Added
- **Runtime Terminal:** Implemented an IMGUI-based runtime terminal for executing commands outside of the Unity Editor.
- **Automatic Command Discovery:** Implemented a `CommandDiscoverer` that automatically registers `static` methods as commands by adding a `[Register]` attribute.
- **Dynamic Delegate Generation:** Implemented a `CommandFactory` that uses Expression Trees to dynamically generate high-performance `CommandHandler` delegates from discovered methods.
- **Strongly-Typed Argument Support:**
    - Implemented automatic type conversion and validation, allowing primitive types like `int`, `float`, and `bool` to be used directly as command arguments.
    - Introduced custom exceptions (`CommandFormatException`, `CommandArgumentException`) to provide detailed error reports on conversion or argument count failures.
- **Manual Command Registration:** Added the ability to manually register instance methods as commands via the `Factory`, in addition to static methods.
- **Input Helpers:**
    - Command history recall (Up/Down arrow keys).
    - Command name auto-completion (Tab key).
- **Dual Input System Support:**
    - Supports both Unity's legacy Input Manager and the new Input System package.
    - Uses Assembly Definition `Define Constraints` to automatically switch implementations based on the project's input settings.
- **Clean Architecture:**
    - Adopted a clean architecture with 9 distinct assemblies (Domain, Application, UI, etc.) to ensure a clean, one-way dependency flow.
- **Customization:**
    - The `TerminalBootstrapper` component allows for customization of fonts, colors, layout, and key bindings directly from the Inspector.