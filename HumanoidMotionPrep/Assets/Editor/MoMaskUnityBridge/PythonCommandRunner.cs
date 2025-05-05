using System;
using System.Diagnostics;
using System.IO;

namespace gianmarcolelli.MoMaskUnityBridge.Editor
{
    public class PythonCommandRunner
    {
        public string PythonPath { get; }
        public string ScriptName { get; }
        public string WorkingDirectory { get; }

        public PythonCommandRunner(string pythonPath, string scriptName, string workingDirectory)
        {
            PythonPath = pythonPath;
            ScriptName = scriptName;
            WorkingDirectory = workingDirectory;
        }

        /// <summary>
        /// Validates whether the specified working directory exists in the file system.
        /// </summary>
        /// <returns>
        /// True if the working directory exists; otherwise, false.
        /// </returns>
        public bool ValidateWorkingDirectory()
        {
            return Directory.Exists(WorkingDirectory);
        }


        /// <summary>
        /// Builds a command string to execute the Python script with the specified parameters.
        /// </summary>
        /// <param name="gpuId">The ID of the GPU to be used by the script.</param>
        /// <param name="extension">The file extension for the output file.</param>
        /// <param name="textPrompt">The text prompt to be passed as an argument to the script.</param>
        /// <returns>
        /// A formatted string containing the command to execute the Python script with the provided arguments.
        /// </returns>
        public string BuildCommand(string gpuId, string extension, string textPrompt)
        {
            return string.Format(
                "{0} --gpu_id {1} --ext {2} --text_prompt \"{3}\"#NA",
                ScriptName, gpuId, extension, textPrompt
            );
        }

        /// <summary>
        /// Executes a Python script with the specified arguments and captures its output.
        /// </summary>
        /// <param name="arguments">The arguments to pass to the Python script.</param>
        /// <returns>
        /// The standard output of the Python script if it executes successfully.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if the Python script exits with a non-zero exit code, indicating an error.
        /// The exception message includes the standard error output from the script.
        /// </exception>
        public string ExecuteCommand(string arguments)
        {
            // Configure the process start information for executing the Python script
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = PythonPath,                  // Path to the Python interpreter
                Arguments = arguments,                  // Command-line arguments for the script
                WorkingDirectory = WorkingDirectory,    // Set the working directory for the process
                CreateNoWindow = false,                  // Do not create a new console window
                UseShellExecute = false,                // Do not use the operating system shell to start the process
                RedirectStandardOutput = true,          // Redirect standard output
                RedirectStandardError = true            // Redirect standard error
            };

            // Use a `Process` to execute the command and handle its input/output streams
            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();

                // Read the standard output and standard error streams
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                // Wait for the process to complete execution
                process.WaitForExit();

                // Check the exit code to determine if the process executed successfully
                if (process.ExitCode != 0)
                {
                    // Throw an exception if the script failed, including the error message
                    throw new Exception("Python script failed with errors:\n" + error);
                }

                // Return the standard output if the script executed successfully
                return output;
            }
        }


        /// <summary>
        /// Retrieves a specific file from a target directory based on its extension and index.
        /// </summary>
        /// <param name="extension">The file extension used to locate the target directory.</param>
        /// <param name="fileIndex">The zero-based index of the desired file within the directory.</param>
        /// <returns>
        /// The full path of the file at the specified index within the target directory.
        /// </returns>
        public string GetSpecificFile(string extension, int fileIndex)
        {
            // Construct the path to the target directory based on the provided extension
            string targetDirectory = Path.Combine(WorkingDirectory, "generation", extension, "animations", "0");

            // Validate if the target directory exists
            if (!Directory.Exists(targetDirectory))
            {
                throw new DirectoryNotFoundException("Target directory not found: " + targetDirectory);
            }

            // Retrieve all files from the target directory
            string[] files = Directory.GetFiles(targetDirectory);

            // Check if the requested file index is within the bounds of the file array
            if (files.Length < fileIndex + 1)
            {
                throw new FileNotFoundException($"File at index {fileIndex} does not exist in {targetDirectory}");
            }

            // Return the path of the file at the specified index
            return files[fileIndex];
        }

    }
}