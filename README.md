# PureTunes: Radio Ad Removal Tool

PureTunes is a C# .NET application designed to remove specific advertisements from 
radio broadcasts using audio fingerprinting. The program removes ads from a provided 
audio file of a radio broadcast by utilizing the technique of audio fingerprinting 
through the powerful [soundfingerprinting](https://github.com/AddictedCS/soundfingerprinting) 
library.

![ui-main-fingerprinted](https://github.com/Serek16/PureTunes/assets/50013854/407b25a7-9f7a-46b8-86ff-d88724b4cfd2)

## How it Works

PureTunes works by comparing the audio fingerprints of the provided radio broadcast 
with those of the supplied advertisement audio files. Once identified, the tool 
eliminates the ads seamlessly from the broadcast audio, providing an uninterrupted 
listening experience.

## Requirements

- .NET Core 7.0
- Docker (for running the `soundfingerprinting.emy` container)
- FFmpeg binaries. Recommended: [ffmpeg-5.1.2-full_build-shared](https://github.com/GyanD/codexffmpeg/releases/download/5.1.2/ffmpeg-5.1.2-full_build-shared.7z) 

## Installation and Usage

1. **Clone the Repository and navigate to the Root Directory:**
   ```bash
   git clone https://github.com/Serek16/PureTunes.git
   cd PureTunes
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
   cd PureTunes
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
   - **Windows:**
      ```bash
      docker run -d -v $Env:USERPROFILE/FingerprintsStorage:/app/data -p 3399:3399 -p 3340:3340 addictedcs/soundfingerprinting.emy:latest
      ```
   
   - **Linux:**
      ```bash
      docker run -d -v ~/FingerprintsStorage:/app/data -p 3399:3399 -p 3340:3340 addictedcs/soundfingerprinting.emy:latest
      ```

7. **Install FFmpeg:**
   - **Windows:** Download FFmpeg libraries for x64 platform. Place the executable files in the following folder:
   C:\FFmpeg\bin\x64. The recommended version is [ffmpeg-5.1.2-full_build-shared](https://github.com/GyanD/codexffmpeg/releases/download/5.1.2/ffmpeg-5.1.2-full_build-shared.7z).
   </br></br>
   - **Linux:**
      ```bash
      sudo apt-get install ffmpeg
      ```

8. **Run the Project:**
   ```bash
   dotnet run
   ```

9. **Access the Web UI:** Open your web browser and navigate to http://localhost:5000 to access the user interface.


## License

This project is made available under the MIT license. More information can be found in the [LICENSE](LICENSE) file.

