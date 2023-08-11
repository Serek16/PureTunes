# AdScrubRadio: Radio Ad Removal Tool

AdScrubRadio is a C# .NET application designed to remove specific advertisements from
radio broadcasts using audio fingerprinting. The program removes ads from a provided 
audio file of a radio broadcast. Utilizing the technique of audio fingerprinting 
through the powerful [soundfingerprinting](https://github.com/AddictedCS/soundfingerprinting)
library, the tool can identify ads that are also supplied in the form of audio files 
and eliminate them.

## Requirements

- .NET Core 3.1 or higher
- Docker (for running the `soundfingerprinting.emy` container)

## Installation and Running

1. **Clone the Repository**: Clone this repository to your local machine.

2. **Edit Configuration File**: Open the `server/appsettings.json` file and edit it with the appropriate paths:

   ```json
   {
   "Dataset": "path to the folder with radio broadcast files",
   "ExaminedFileDataset": "path to the folder with subfolders containing ads",
   "OutPath": "path to the folder where the results are to be generated"
   }
   ```

3. **Build the Project**: Run the following command:

   ```bash
   dotnet build
   ```

4. **Run the SoundFingerprinting Container:**
   ```bash
   docker run -d -v /persistent-dir:/app/data -p 3399:3399 -p 3340:3340 addictedcs/soundfingerprinting.emy:latest
   ```

5. **Run the Program**: Navigate to server directory then run the program using the following command:

   ```bash
   cd server
   dotnet run
   ```

6. **Access the Web UI**: Open your web browser and navigate to http://localhost:5000 to access the user interface.


## License

This project is made available under the MIT license. More information can be found in the LICENSE file.

