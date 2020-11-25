# ResetOrNot
Displays if you should reset or not at a given moment of time. Uses some fancy math to get it done.

## Component in action:
![screenshot](https://user-images.githubusercontent.com/55288842/100280825-db327f00-2f79-11eb-82c2-e042c95ede36.png)

## Installation:

1. Place [ResetOrNot.dll](https://github.com/gottagofaster236/ResetOrNot/releases/latest) into the Components directory of your LiveSplit installation.
2. Open LiveSplit. Right click -> Edit Layout -> [Giant "+" Button] -> Information -> ResetOrNot
3. You can configure how many of your most recent attempts will be used to calculate whether you should reset or not. Go to Layout Settings and click on the ResetOrNot tab. You can either have it use a percentage of your most recent attempts, or just a fixed number of your most recent attempts.
4. Speedrun!

## Troubleshooting:

- **It always displays "N/A"**<br>
You may need to configure the plugin to use a different number of attempts. For instance, it may not be reading any attempts in which you've completed a run. Additionally, you may have reset your split data at some point, which will remove the data necessary for ResetOrNot to calculate its probability.

- **It is displaying "Calculating..." for a long time**<br>
Right now the algorithm is suboptimal, resulting in a _O(n<sup>2</sup>)_ time complexity, where _n_ is the number of splits.<br>
You can always continue your run while the plugin is doing its work ;)

## Building
Right now the repo contains many files that should be in gitignore ;) Anyways.

You'll need an up-to-date version of Visual Studio. Clone the [LiveSplit repository](https://github.com/LiveSplit/LiveSplit), then clone this repository. Now you should be good to go!
