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





Local Development Setup

To run the project locally, you need to initialize and set up the required secrets. Navigate to the LinkTracker.Bot directory and run the following commands:

    Initialize User Secrets:
    Bash

dotnet user-secrets init

Configure the Bot Token:
Replace <your-telegram-token> with the token provided by @BotFather.
Bash

dotnet user-secrets set "BotConfiguration:BotToken" "<your-telegram-token>"

Configure the Scrapper URL:
Set the base address for the Scrapper service (usually http://localhost:5000 or similar):
Bash

    dotnet user-secrets set "BotConfiguration:ScrapperUrl" "http://localhost:5000"

Verifying Secrets

You can verify your configured secrets by running:
Bash

dotnet user-secrets list

    Note: User Secrets are only used during development. For production environments, these values should be provided via Environment Variables.