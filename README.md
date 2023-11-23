# AdScrubRadio: Radio Ad Removal Tool

AdScrubRadio is a C# .NET application designed to remove specific advertisements from
radio broadcasts using audio fingerprinting. The program removes ads from a provided 
audio file of a radio broadcast. Utilizing the technique of audio fingerprinting 
through the powerful [soundfingerprinting](https://github.com/AddictedCS/soundfingerprinting)
library, the tool can identify ads that are also supplied in the form of audio files 
and eliminate them.

## Requirements

- .NET Core 7.0
- Docker (for running the `soundfingerprinting.emy` container)

## Installation and Running

1. **Clone the Repository and navigate to the Root Directory:**
   ```bash
   git clone https://github.com/Serek16/EmySoundProject.git
   cd EmySoundProject
   ```

2. **Edit Configuration File**: Open the `EmySoundProject/appsettings.json` file and edit it with the appropriate paths:
   ```json
   {
    "Dataset": "path to the folder with radio broadcast files",
    "ExaminedFileDataset": "path to the folder with subfolders containing ads",
    "OutPath": "path to the folder where the results are to be generated"
   }
   ```

3. **Navigate to the Project Directory:**
   ```bash
   cd EmySoundProject
   ```

4. **Restore NuGet Packages:** 
   ```bash
   dotnet restore
   ```

5. **Build the Project:**
   ```bash
   dotnet build
   ```

6. **Run the SoundFingerprinting Docker container:**
   ```bash
   docker run -d -v /persistent-dir:/app/data -p 3399:3399 -p 3340:3340 addictedcs/soundfingerprinting.emy:latest
   ```

7. **Run the Project:**
   ```bash
   dotnet run
   ```

8. **Access the Web UI:** Open your web browser and navigate to http://localhost:5000 to access the user interface.


## License

This project is made available under the MIT license. More information can be found in the [LICENSE](LICENSE) file.

