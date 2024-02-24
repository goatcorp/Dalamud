void ConsoleSetup(const std::wstring console_name);
void ConsoleTeardown();

HRESULT InitializeClrAndGetEntryPoint(
    void* calling_module,
    bool enable_etw,
    std::wstring runtimeconfig_path,
    std::wstring module_path,
    std::wstring entrypoint_assembly_name,
    std::wstring entrypoint_method_name,
    std::wstring entrypoint_delegate_type_name,
    void** entrypoint_fn);
