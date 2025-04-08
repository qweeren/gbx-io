using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Net.Http;
using System.Text;

namespace GbxIo.Desktop;

public partial class MainForm : Form
{
    private WebView2 webView;
    private Process webServerProcess;

    public MainForm()
    {
        InitializeComponent();
        StartWebServer();
        InitializeAsync();
    }

    private void StartWebServer()
    {
        try
        {
            // Start the web server process
            webServerProcess = new Process();
            webServerProcess.StartInfo.FileName = "dotnet";

            // Get the absolute path to the GbxIo.PWA project
            string pwaProjectPath = FindPwaProjectPath();

            if (string.IsNullOrEmpty(pwaProjectPath))
            {
                MessageBox.Show("Could not find the GbxIo.PWA project. Please make sure the project exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            // Configure the process to capture output
            Debug.WriteLine($"Starting web server with project: {pwaProjectPath}");
            webServerProcess.StartInfo.Arguments = $"run --project \"{pwaProjectPath}\" --urls http://localhost:5004";
            webServerProcess.StartInfo.UseShellExecute = false;
            webServerProcess.StartInfo.CreateNoWindow = true;
            webServerProcess.StartInfo.RedirectStandardOutput = true;
            webServerProcess.StartInfo.RedirectStandardError = true;
            webServerProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(pwaProjectPath);

            // Set up output handlers
            StringBuilder outputBuilder = new StringBuilder();
            webServerProcess.OutputDataReceived += (sender, args) => {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    outputBuilder.AppendLine(args.Data);
                    Debug.WriteLine($"Web Server: {args.Data}");
                }
            };

            webServerProcess.ErrorDataReceived += (sender, args) => {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    outputBuilder.AppendLine($"ERROR: {args.Data}");
                    Debug.WriteLine($"Web Server Error: {args.Data}");
                }
            };

            // Start the process
            webServerProcess.Start();
            webServerProcess.BeginOutputReadLine();
            webServerProcess.BeginErrorReadLine();

            // Wait for the server to start and verify it's running
            bool serverStarted = WaitForServerToStart("http://localhost:5004", 30);

            if (!serverStarted)
            {
                string errorMessage = $"Failed to start the web server. Output:\n{outputBuilder}";
                MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Try to kill the process if it's still running
                try { webServerProcess?.Kill(); } catch { }

                Application.Exit();
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error starting the web server: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    private async void InitializeAsync()
    {
        try
        {
            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            Controls.Add(webView);

            // Add navigation completed handler to check for errors
            webView.CoreWebView2InitializationCompleted += (sender, e) =>
            {
                if (e.IsSuccess)
                {
                    // Configure web view settings
                    webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                    webView.CoreWebView2.Settings.IsStatusBarEnabled = true;

                    // Handle navigation errors
                    webView.CoreWebView2.NavigationCompleted += (s, args) =>
                    {
                        if (!args.IsSuccess)
                        {
                            string errorMessage = $"Failed to navigate to the web application. Error code: {args.WebErrorStatus}\n" +
                                                "Please make sure the web server is running correctly.";
                            MessageBox.Show(errorMessage, "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            // Try to reload after a short delay
                            Task.Delay(3000).ContinueWith(_ => {
                                this.Invoke(() => {
                                    webView.CoreWebView2.Reload();
                                });
                            });
                        }
                    };

                    // Add console message handler for debugging
                    webView.CoreWebView2.WebMessageReceived += (s, args) => {
                        Debug.WriteLine($"Web Message: {args.WebMessageAsJson}");
                    };
                }
                else
                {
                    MessageBox.Show($"Failed to initialize WebView2: {e.InitializationException?.Message}",
                        "WebView2 Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.Navigate("http://localhost:5004");

            Text = "GbxIo Desktop";
            WindowState = FormWindowState.Maximized;

            // Use try-catch for loading the icon in case the path is incorrect
            try
            {
                string iconPath = FindIconPath();
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    Icon = new Icon(iconPath);
                }
            }
            catch
            {
                // Ignore icon loading errors
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error initializing the web view: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);

        // Kill the web server process when the form is closed
        if (webServerProcess != null && !webServerProcess.HasExited)
        {
            webServerProcess.Kill();
        }
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        //
        // MainForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1200, 800);
        this.Name = "MainForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.ResumeLayout(false);
    }

    /// <summary>
    /// Finds the path to the GbxIo.PWA project by trying multiple possible locations
    /// </summary>
    /// <returns>The full path to the GbxIo.PWA.csproj file, or null if not found</returns>
    private string FindPwaProjectPath()
    {
        string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string pwaProjectName = "GbxIo.PWA";
        string projectFileName = $"{pwaProjectName}.csproj";

        // Try multiple possible directory structures to find the PWA project
        List<string> possiblePaths = new List<string>();

        // Try going up 4 levels (typical for bin/Debug/net9.0-windows/app.exe structure)
        possiblePaths.Add(Path.GetFullPath(Path.Combine(currentDirectory, "..\\..\\..\\..\\", pwaProjectName, projectFileName)));

        // Try going up 3 levels (alternative structure)
        possiblePaths.Add(Path.GetFullPath(Path.Combine(currentDirectory, "..\\..\\..\\" , pwaProjectName, projectFileName)));

        // Try going up 5 levels (another possible structure)
        possiblePaths.Add(Path.GetFullPath(Path.Combine(currentDirectory, "..\\..\\..\\..\\..\\", pwaProjectName, projectFileName)));

        // Try to find the solution file and then locate the project relative to it
        string solutionDirectory = FindSolutionDirectory(currentDirectory);
        if (!string.IsNullOrEmpty(solutionDirectory))
        {
            possiblePaths.Add(Path.Combine(solutionDirectory, pwaProjectName, projectFileName));
        }

        // Try each path until we find one that exists
        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                Debug.WriteLine($"Found PWA project at: {path}");
                return path;
            }
        }

        // If we still haven't found it, try searching for it
        string result = TryFallbackProjectSearch(currentDirectory, projectFileName);
        return result;
    }

    /// <summary>
    /// Tries additional fallback methods to find the project file
    /// </summary>
    /// <param name="currentDirectory">Directory to start searching from</param>
    /// <param name="projectFileName">The project file name to search for</param>
    /// <returns>The full path to the project file if found, or null if not found</returns>
    private string TryFallbackProjectSearch(string currentDirectory, string projectFileName)
    {
        // First try the standard search
        string searchResult = SearchForProjectFile(currentDirectory, projectFileName, 6); // Search up to 6 levels up
        if (!string.IsNullOrEmpty(searchResult))
        {
            Debug.WriteLine($"Found PWA project by search at: {searchResult}");
            return searchResult;
        }

        // Last resort: try to find any .csproj file with PWA in the name
        string anyPwaProject = SearchForProjectFile(currentDirectory, "*PWA*.csproj", 6);
        if (!string.IsNullOrEmpty(anyPwaProject))
        {
            Debug.WriteLine($"Found alternative PWA project at: {anyPwaProject}");
            return anyPwaProject;
        }

        Debug.WriteLine("Could not find PWA project anywhere");
        return null;
    }

    /// <summary>
    /// Attempts to find the solution directory by looking for a .sln file
    /// </summary>
    /// <param name="startDirectory">Directory to start searching from</param>
    /// <returns>The solution directory path, or null if not found</returns>
    private string FindSolutionDirectory(string startDirectory)
    {
        string directory = startDirectory;

        // Go up to 6 levels looking for a .sln file
        for (int i = 0; i < 6; i++)
        {
            if (Directory.GetFiles(directory, "*.sln").Length > 0)
            {
                return directory;
            }

            string parentDir = Directory.GetParent(directory)?.FullName;
            if (parentDir == null)
            {
                break;
            }

            directory = parentDir;
        }

        return null;
    }

    /// <summary>
    /// Recursively searches for a specific file starting from a directory and going up
    /// </summary>
    /// <param name="startDirectory">Directory to start searching from</param>
    /// <param name="fileName">File name to search for</param>
    /// <param name="maxLevels">Maximum number of directory levels to search up</param>
    /// <returns>The full path to the file if found, or null if not found</returns>
    private string SearchForProjectFile(string startDirectory, string fileName, int maxLevels)
    {
        if (maxLevels <= 0)
        {
            return null;
        }

        // Search in the current directory and all subdirectories
        foreach (string file in Directory.GetFiles(startDirectory, fileName, SearchOption.AllDirectories))
        {
            return file;
        }

        // Go up one level and try again
        string parentDir = Directory.GetParent(startDirectory)?.FullName;
        if (parentDir != null)
        {
            return SearchForProjectFile(parentDir, fileName, maxLevels - 1);
        }

        return null;
    }

    /// <summary>
    /// Finds the path to the application icon by trying multiple possible locations
    /// </summary>
    /// <returns>The full path to the icon file, or null if not found</returns>
    private string FindIconPath()
    {
        string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string iconFileName = "appicon.ico";

        // Try multiple possible paths for the icon
        List<string> possiblePaths = new List<string>
        {
            // Try the standard Resources folder
            Path.Combine(currentDirectory, "Resources", "AppIcon", iconFileName),

            // Try relative to the executable
            Path.Combine(currentDirectory, iconFileName),

            // Try in the parent directory
            Path.Combine(Directory.GetParent(currentDirectory)?.FullName ?? string.Empty, "Resources", "AppIcon", iconFileName),

            // Try with forward slashes (cross-platform style)
            Path.Combine(currentDirectory, "Resources/AppIcon", iconFileName)
        };

        // Try to find the solution directory and look for the icon there
        string solutionDirectory = FindSolutionDirectory(currentDirectory);
        if (!string.IsNullOrEmpty(solutionDirectory))
        {
            possiblePaths.Add(Path.Combine(solutionDirectory, "GbxIo.Desktop", "Resources", "AppIcon", iconFileName));
            possiblePaths.Add(Path.Combine(solutionDirectory, "Resources", "AppIcon", iconFileName));
        }

        // Try each path until we find one that exists
        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // If we still haven't found it, try searching for it
        return SearchForFile(currentDirectory, iconFileName, 6); // Search up to 6 levels up
    }

    /// <summary>
    /// Searches for a specific file in the directory structure
    /// </summary>
    /// <param name="startDirectory">Directory to start searching from</param>
    /// <param name="fileName">File name to search for</param>
    /// <param name="maxLevels">Maximum number of directory levels to search up</param>
    /// <returns>The full path to the file if found, or null if not found</returns>
    private string SearchForFile(string startDirectory, string fileName, int maxLevels)
    {
        if (maxLevels <= 0 || string.IsNullOrEmpty(startDirectory))
        {
            return null;
        }

        try
        {
            // Search in the current directory and all subdirectories
            string[] files = Directory.GetFiles(startDirectory, fileName, SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                return files[0];
            }

            // Go up one level and try again
            string parentDir = Directory.GetParent(startDirectory)?.FullName;
            if (parentDir != null)
            {
                return SearchForFile(parentDir, fileName, maxLevels - 1);
            }
        }
        catch
        {
            // Ignore any errors during search
        }

        return null;
    }

    /// <summary>
    /// Waits for the web server to start and become available
    /// </summary>
    /// <param name="url">The URL to check</param>
    /// <param name="timeoutSeconds">Maximum time to wait in seconds</param>
    /// <returns>True if the server started successfully, false otherwise</returns>
    private bool WaitForServerToStart(string url, int timeoutSeconds)
    {
        DateTime startTime = DateTime.Now;
        DateTime endTime = startTime.AddSeconds(timeoutSeconds);

        while (DateTime.Now < endTime)
        {
            try
            {
                // Try to connect to the server
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(2);
                    var task = client.GetAsync(url);

                    // Wait for the task to complete or timeout
                    if (task.Wait(2000))
                    {
                        var response = task.Result;
                        if (response.IsSuccessStatusCode)
                        {
                            Debug.WriteLine($"Server started successfully at {url}");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Server not yet available: {ex.Message}");
            }

            // Wait a bit before trying again
            System.Threading.Thread.Sleep(500);
        }

        Debug.WriteLine($"Server failed to start within {timeoutSeconds} seconds");
        return false;
    }
}
