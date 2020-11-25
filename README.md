# ResetOrNot
Displays whether you should reset or not at a given moment of time.

## Component in action:
![screenshot](https://user-images.githubusercontent.com/55288842/100280825-db327f00-2f79-11eb-82c2-e042c95ede36.png)

## Installation:

1. Place [ResetOrNot.dll](https://github.com/gottagofaster236/ResetOrNot/releases/latest) into the Components directory of your LiveSplit installation.
2. Open LiveSplit. Right click -> Edit Layout -> [Giant "+" Button] -> Information -> ResetOrNot
3. You can configure how many of your most recent attempts will be used to calculate whether you should reset or not. Go to Layout Settings and click on the ResetOrNot tab. You can either have it use a percentage of your most recent attempts, or just a fixed number of your most recent attempts.
4. Speedrun!

## Troubleshooting:

**It always displays "N/A"**

You may need to configure the plugin to use a different number of attempts. For instance, it may not be reading any attempts in which you've completed a run. Additionally, you may have reset your split data at some point, which will remove the data necessary for ResetOrNot to calculate its probability.
