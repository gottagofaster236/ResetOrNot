# ResetOrNot
Displays whether you should reset or not at a given moment of time.

## Installation:

1. Place ResetOrNot.dll into the Components directory of your LiveSplit installation.
2. Open LiveSplit. Right click -> Edit Layout -> [Giant "+" Button] -> Information -> PB Chance
3. You can configure how many of your most recent attempts will be used to calculate the PB chance. Go to Layout Settings and click on the PB Chance tab. You can either have it use a percentage of your most recent attempts, or just a fixed number of your most recent attempts.
4. Speedrun!

## Troubleshooting:

**It always displays "0%" or "-"**

You may need to configure the plugin to use a different number of attempts. For instance, it may not be reading any attempts in which you've completed a run. Additionally, you may have reset your split data at some point, which will remove the data necessary for ResetOrNot to calculate its probability. If you want to debug the issue, try opening your splits file in a text editor (it's XML formatted). You may be able to spot missing splits, and it will inform you how to configure the ResetOrNot component.
