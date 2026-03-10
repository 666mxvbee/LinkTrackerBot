# LinkTracker Bot





## Installation



1. Clone the repository:


   ```bash


   git clone https://github.com/yourusername/LinkTracker.Bot.git


   cd LinkTracker.Bot


   ```




2. Restore dependencies:



   ```bash


   dotnet restore


   ```





3. Configure user secrets (see Configuration section below).





4. Build the project:


   ```bash


   dotnet build


   ```





## Configuration





This project uses .NET user secrets to securely store sensitive configuration data like API keys and tokens. User secrets are stored locally and are not committed to version control.





### Setting Up User Secrets





1. Initialize user secrets for the project:


   ```bash


   dotnet user-secrets init


   ```





2. Set your bot token and other secrets:


   ```bash


   dotnet user-secrets set "BotOptions:Token" "your-bot-token-here"


   dotnet user-secrets set "BotOptions:ApiKey" "your-api-key-here"


   ```





<<<<<<< HEAD
Replace `"your-bot-token-here"` and `"your-api-key-here"` with your actual values.
=======
   Replace `"your-bot-token-here"` and `"your-api-key-here"` with your actual values.
>>>>>>> 98b4592f243f7dc9b32d755ae43aa06e836bcdc0
