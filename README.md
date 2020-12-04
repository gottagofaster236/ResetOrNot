# ResetOrNot
Displays if you should reset or not at a given moment of time. Uses some fancy math to get it done.

## Component in action:
![screenshot](https://user-images.githubusercontent.com/55288842/101182782-9dabb100-365f-11eb-949a-89078f0fd240.png)

## Installation:

1. Place [ResetOrNot.dll](https://github.com/gottagofaster236/ResetOrNot/releases/latest) into the Components directory of your LiveSplit installation.
2. Open LiveSplit. Right click -> Edit Layout -> [Giant "+" Button] -> Information -> ResetOrNot
3. You can configure how many of your most recent attempts will be used to calculate whether you should reset or not. Go to Layout Settings and click on the ResetOrNot tab. You can either have it use a percentage of your most recent attempts, or just a fixed number of your most recent attempts.
4. Speedrun!

## Configuration screen:
![configuration_screen](https://user-images.githubusercontent.com/55288842/101182810-a9977300-365f-11eb-8d0a-8ce52eb48fa0.png)
)

## Troubleshooting:

- **It always displays "N/A"**<br>
You may need to configure the plugin to use a different number of attempts. For instance, it may not be reading any attempts in which you've completed a run. Additionally, you may have reset your split data at some point, which will remove the data necessary for ResetOrNot to calculate its probability.

## Building
You'll need an up-to-date version of Visual Studio. Clone the [LiveSplit repository](https://github.com/LiveSplit/LiveSplit), then clone this repository. Now you should be good to go!
