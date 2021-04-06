# SUNLootChecker

The SUN Loot Checker will compare AO LootLogs with ChestLogs and output missing items and players who looted them.

## Installing SUN Loot Checker
1. Download the newest Release under https://github.com/scorixear/SUNLootChecker/releases
2. Extract the files into a location you seem fit
3. Execute the SUNLootChecker.exe file

## Pasting AO LootLogs
1. Go to https://www.aolootlog.com and download your LootLog as .txt file.
2. Copy the contents of the .txt file into the "Player Loot" Textbox in the SUN Loot Checker.

## Pasting ChestLogs
1. Open Albion and the specific ChestLogs you like to compare to.
2. Make sure your Albion Language is set on English.
3. Edit the filter to your timespan (most likely 24 hours).
4. Change the Filter to only show "Deposits".
5. Click on the "Copy to Clipboard" Icon.
6. Paste the ChestLog into the "Chest Log" Textbox in the SUN Loot Checker.
7. Optionally add additional Chestlogs below the previous chestlog in the SUN Loot Checker. Make sure, that you always start on a new line.

## Running the Loot Checker
Click on "Check Loot" and wait until the Progressbar is full.
Below the Check Loot button the results will appear.
These results are orderd by Player, amount of Items they looted and actual items they looted.
By clicking on the Headers, you can sort this table.

## Understanding the Results
Each Entry is divided by PlayerName. The second column describes the amount of unique itemtypes this player has looted. This will mostlikely match with the amount of items the player looted.
The third Column is a list of every item they looted and did not put into the chest.
The Result can be understood like this:

ItemName | Tier | Amount

## Additional Information behind the Scenes
The Loot Checker solves the Issue that AO LootLog uses Albion API Itemnames and the Chestlog uses Ingame Names.
Therefore the LootChecker creates a dictionary that is named "ItemDic.json" and can be found in the directory of the .exe file.
This Dictionary is expanded on need. It is initially filled with general items, but will expand itself once unknown relations are entered.
This means, the SUNLootChecker will request the Ingame Name of each unkown item via the Albion API. This might take a while. Once requested, the result is stored.
Subsequent Runs of the Programm will go much faster.
Make sure you do not delete the "ItemDic.json" file. Otherwise, your checks will go much slower.



## Known Issues
- The Result Table cannot be scrolled if the mouse hovers over the Items Column. 
