using System;
using System.Diagnostics;
using System.IO;
using SoulsFormats;
class Program
{
    static void Main(string[] args)
    {
        // Check if the user dragged a folder into the application
        if (args.Length == 0 || !Directory.Exists(args[0]))
        {
            Console.WriteLine("Please drag and drop a folder onto the executable.");
            return;
        }

        // Get the folder path from the user's drag-and-drop input
        string directoryPath = args[0];

        // Get the path of the currently running executable and find texconv and texdiag in the same directory
        string exeFolderPath = AppDomain.CurrentDomain.BaseDirectory;
        string texconvPath = Path.Combine(exeFolderPath, "texconv.exe");
        string texdiagPath = Path.Combine(exeFolderPath, "texdiag.exe");

        // Function to process a TPF (rename textures and resize them)
        void ProcessTPF(TPF tpf)
        {
            for (int i = 0; i < tpf.Textures.Count; i++)
            {
                if (!tpf.Textures[i].Name.EndsWith("_l"))
                {
                    tpf.Textures[i].Name += "_l";
                }

                // Extract DDS from the embedded texture
                string extractedTexturePath = Path.Combine(directoryPath, tpf.Textures[i].Name + ".dds");
                File.WriteAllBytes(extractedTexturePath, tpf.Textures[i].Bytes);

                // Step 1: Run texdiag to get the texture information (width and height)
                ProcessStartInfo texdiagInfo = new ProcessStartInfo
                {
                    FileName = texdiagPath,
                    Arguments = $"info \"{extractedTexturePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                int originalWidth = 0;
                int originalHeight = 0;

                using (Process texdiagProcess = Process.Start(texdiagInfo))
                {
                    string diagOutput = texdiagProcess.StandardOutput.ReadToEnd();
                    texdiagProcess.WaitForExit();

                    // Parse texdiag output to extract original resolution
                    foreach (string line in diagOutput.Split('\n'))
                    {
                        if (line.Trim().StartsWith("width ="))
                        {
                            originalWidth = int.Parse(line.Split('=')[1].Trim());
                        }
                        else if (line.Trim().StartsWith("height ="))
                        {
                            originalHeight = int.Parse(line.Split('=')[1].Trim());
                        }
                    }
                }

                // Step 2: Calculate the new resolution (half the original size)
                if (originalWidth > 0 && originalHeight > 0)
                {
                    int newWidth = originalWidth / 2;
                    int newHeight = originalHeight / 2;

                    // Step 3: Run texconv to resize the DDS file
                    ProcessStartInfo texconvInfo = new ProcessStartInfo
                    {
                        FileName = texconvPath,
                        Arguments = $"-nologo -ft dds -o \"{directoryPath}\" -w {newWidth} -h {newHeight} -y \"{extractedTexturePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process texconvProcess = Process.Start(texconvInfo))
                    {
                        string texconvOutput = texconvProcess.StandardOutput.ReadToEnd();
                        string texconvErrors = texconvProcess.StandardError.ReadToEnd();
                        texconvProcess.WaitForExit();

                        // Check for errors
                        if (!string.IsNullOrEmpty(texconvErrors))
                        {
                            Console.WriteLine($"Error resizing texture {extractedTexturePath}: {texconvErrors}");
                        }
                    }

                    // Step 4: Reinsert the resized DDS back into the TPF
                    tpf.Textures[i].Bytes = File.ReadAllBytes(extractedTexturePath);

                    // Optionally, delete the extracted texture after reinsertion
                    File.Delete(extractedTexturePath);
                }
                else
                {
                    Console.WriteLine($"Failed to determine original resolution for {extractedTexturePath}");
                }
            }
        }

        // Get all .tpf.dcx, .texbnd.dcx, and .partsbnd.dcx files
        string[] files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

        // Loop through all files
        foreach (string file in files)
        {
            // Handle .tpf.dcx files
            if (file.EndsWith(".tpf.dcx") && !file.EndsWith("_l.tpf.dcx"))
            {
                TPF tpf = TPF.Read(file);

                // Process the TPF file
                ProcessTPF(tpf);

                // Create the new file name with "_l" before ".tpf.dcx"
                string fileNameWithoutExtension = Path.GetFileName(file).Replace(".tpf.dcx", "");
                string newFileName = Path.Combine(Path.GetDirectoryName(file), fileNameWithoutExtension + "_l.tpf.dcx");

                // Write the modified TPF to the new file
                tpf.Write(newFileName);

                // Print message when done with .tpf.dcx file
                Console.WriteLine($"Processed .tpf.dcx file: {file}");
            }

            // Handle .texbnd.dcx files
            else if (file.EndsWith("_h.texbnd.dcx"))
            {
                // Read the .texbnd.dcx file as BND4
                BND4 texBnd = BND4.Read(file);

                // Loop through the files in the BND4 bundle
                foreach (BinderFile bndFile in texBnd.Files)
                {
                    // Check if the file is a .tpf file and its name ends with "_h"
                    if (bndFile.Name.EndsWith("_h.tpf"))
                    {
                        // Rename the .tpf file from _h to _l
                        string newTpfName = bndFile.Name.Replace("_h.tpf", "_l.tpf");

                        // Load the TPF content from the BinderFile's Bytes
                        TPF tpf = TPF.Read(bndFile.Bytes);

                        // Process the TPF file
                        ProcessTPF(tpf);

                        // Update the BinderFile Bytes with the modified TPF
                        bndFile.Bytes = tpf.Write();

                        // Rename the BinderFile's name from _h.tpf to _l.tpf
                        bndFile.Name = newTpfName;
                    }
                }

                // Write the updated BND4 back to file
                texBnd.Write(file.Replace("_h.texbnd.dcx", "_l.texbnd.dcx"));

                // Print message when done with .texbnd.dcx file
                Console.WriteLine($"Processed .texbnd.dcx file: {file}");
            }

            // Handle .partsbnd.dcx files
            else if (file.EndsWith(".partsbnd.dcx"))
            {
                // Read the .partsbnd.dcx file as BND4
                BND4 partsBnd = BND4.Read(file);

                // Loop through the files in the BND4 bundle
                foreach (BinderFile bndFile in partsBnd.Files)
                {
                    // Check if the file is a .tpf file and its name ends with "_h"
                    if (bndFile.Name.EndsWith(".tpf"))
                    {
                        // Rename the .tpf file from _h to _l
                        string newTpfName = bndFile.Name.Replace(".tpf", "_l.tpf");

                        // Load the TPF content from the BinderFile's Bytes
                        TPF tpf = TPF.Read(bndFile.Bytes);

                        // Process the TPF file
                        ProcessTPF(tpf);

                        // Update the BinderFile Bytes with the modified TPF
                        bndFile.Bytes = tpf.Write();

                        // Rename the BinderFile's name from _h.tpf to _l.tpf
                        bndFile.Name = newTpfName;
                    }
                }

                // Write the updated BND4 back to file
                partsBnd.Write(file.Replace(".partsbnd.dcx", "_l.partsbnd.dcx"));

                // Print message when done with .partsbnd.dcx file
                Console.WriteLine($"Processed .partsbnd.dcx file: {file}");
            }
        }

        // Final message indicating that the tool is done
        Console.WriteLine("All files processed. Press any key to exit.");
        Console.ReadKey();  // Wait for the user to press a key before exiting
    }
}
