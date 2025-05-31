#Requires -Version 5.1

<#
.SYNOPSIS
    Gathers content from specified code files within a project and consolidates them into a single output file.
.DESCRIPTION
    This script prompts the user for the project's root directory and an output file path.
    It then searches for a predefined list of relevant source code and view files (typically
    involved in features like user registration and API interaction in an ASP.NET Core MVC project)
    and appends their content to the output file. Each dumped file's content is clearly marked
    with its original relative path.
.PARAMETER ProjectRootPath
    The absolute path to the root directory of your project (e.g., "C:\Users\prabe\Documents\Class\3rd Year\Modern Programming Language - 2\Music-App-Project\MusicAppFrontend").
.PARAMETER OutputFilePath
    The absolute path for the consolidated output file (e.g., "C:\temp\CollectedCode.txt").
.EXAMPLE
    .\Collect-ProjectCode.ps1
    (The script will then prompt for the ProjectRootPath and OutputFilePath)
#>

function Get-UserInputPath {
    param (
        [string]$PromptMessage,
        [switch]$EnsureDirectoryExists,
        [switch]$EnsureFileDoesNotExistOrOverwrite
    )
    while ($true) {
        $inputPath = Read-Host -Prompt $PromptMessage
        if ([string]::IsNullOrWhiteSpace($inputPath)) {
            Write-Warning "Path cannot be empty. Please try again."
            continue
        }
        if ($EnsureDirectoryExists) {
            if (-not (Test-Path -Path $inputPath -PathType Container)) {
                Write-Warning "Directory '$inputPath' not found. Please enter a valid directory path."
                continue
            }
        }
        if ($EnsureFileDoesNotExistOrOverwrite) {
            if (Test-Path -Path $inputPath -PathType Leaf) {
                $overwrite = Read-Host "File '$inputPath' already exists. Overwrite? (y/n)"
                if ($overwrite -ne 'y') {
                    Write-Host "Please enter a different output file path."
                    continue
                }
            }
        }
        return $inputPath
    }
}

# --- Configuration ---
Write-Host "PowerShell Code File Dumper" -ForegroundColor Yellow
Write-Host "-----------------------------"

# Get Project Root Path from User
$ProjectRootPath = Get-UserInputPath -PromptMessage "Enter the absolute path to your project's root directory (e.g., C:\Projects\MusicAppFrontend)" -EnsureDirectoryExists

# Get Output File Path from User
$OutputFilePath = Get-UserInputPath -PromptMessage "Enter the absolute path for the output file (e.g., C:\temp\MusicAppDump.txt)" -EnsureFileDoesNotExistOrOverwrite

# Define the list of files/patterns to collect (relative to ProjectRootPath)
# These are based on common locations for ASP.NET Core MVC projects and your file structure.
$FilesToCollect = @(
    # Core Services
    "Services/AuthService.cs",
    "Services/ApiService.cs", # If it exists and is used by AuthService

    # Controllers
    "Controllers/AccountController.cs",

    # ViewModels
    "ViewModels/RegisterViewModel.cs",
    "ViewModels/LoginViewModel.cs", # Often related

    # Views - Account
    "Views/Account/Register.cshtml",
    "Views/Account/_RegisterFormPartial.cshtml", # If it exists
    "Views/Account/Login.cshtml",
    "Views/Account/_LoginFormPartial.cshtml", # If it exists

    # Views - Shared (Layout, Alerts, Validation)
    "Views/Shared/_Layout.cshtml",
    "Views/Shared/_AlertMessages.cshtml", # Important for displaying feedback
    "Views/Shared/_ValidationScriptsPartial.cshtml",

    # Client-side scripts (if API calls are made directly from JS)
    "wwwroot/js/api-utils.js",
    "wwwroot/js/site.js", # General site JS, might contain relevant logic

    # Configuration / Startup
    "Program.cs", # For service registration, middleware
    "appsettings.json",
    "appsettings.Development.json",

    # Models (User model is often relevant)
    "Models/User.cs",
    "Models/DTOs/CommonDTOs.cs" # If it contains relevant request/response DTOs
)

# --- Execution ---
# Create or clear the output file
try {
    Set-Content -Path $OutputFilePath -Value "Code Dump for Project: $ProjectRootPath `nDate: $(Get-Date)`n---`n" -ErrorAction Stop
    Write-Host "Output file '$OutputFilePath' created/cleared." -ForegroundColor Green
}
catch {
    Write-Error "Failed to create or clear output file '$OutputFilePath'. Error: $($_.Exception.Message)"
    exit 1
}

$filesProcessedCount = 0
$filesFoundCount = 0

Write-Host "`nStarting to collect files..."

foreach ($RelativeFilePath in $FilesToCollect) {
    $FullFilePath = Join-Path -Path $ProjectRootPath -ChildPath $RelativeFilePath
    Write-Host "Attempting to process: $RelativeFilePath" -ForegroundColor Cyan

    if (Test-Path -Path $FullFilePath -PathType Leaf) {
        try {
            $fileContent = Get-Content -Path $FullFilePath -Raw -ErrorAction Stop
            $header = @"

# ==================================================================================================
# FILE: $RelativeFilePath
# ==================================================================================================

"@
            Add-Content -Path $OutputFilePath -Value ($header + $fileContent) -ErrorAction Stop
            Write-Host "  [SUCCESS] Added content of '$RelativeFilePath' to output file." -ForegroundColor Green
            $filesFoundCount++
        }
        catch {
            Write-Warning "  [ERROR] Could not read or append content for '$RelativeFilePath'. Error: $($_.Exception.Message)"
        }
    }
    else {
        Write-Host "  [INFO] File '$RelativeFilePath' not found at '$FullFilePath'." -ForegroundColor Gray
    }
    $filesProcessedCount++
}

# --- Summary ---
Write-Host "`n--- Processing Complete ---" -ForegroundColor Yellow
Write-Host "Total files/patterns checked: $filesProcessedCount"
Write-Host "Total files found and dumped: $filesFoundCount"
Write-Host "Consolidated code dumped to: $OutputFilePath" -ForegroundColor Green

# Optional: Open the output file
# if ($filesFoundCount -gt 0) {
#     $openFile = Read-Host "Do you want to open the output file now? (y/n)"
#     if ($openFile -eq 'y') {
#         Invoke-Item $OutputFilePath
#     }
# }

Write-Host "Script finished."
