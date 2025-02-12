This program interfaces with the iRacing SDK to get telemetry and other data and serve it to an arbitrary endpoint. It abstracts away the iRacing SDK and delivers the data to your application in a simple way so you can focus on building cool UIs (think RaceLab or iOverlay) or other useful tools. For example, I have built a simple telemetry UI called [open-overlay](https://github.com/dschykerynec/open-overlay) in Electron using this project.

## Why this exists

There are plenty of other projects that abstract away the lower levels of the iRacing SDK. So why am I building another one? The answer is that all of the existing solutions are tech-stack specific. There is a much fuller featured node.js implementation (that is unfortunately no longer actively developed). There is a python implementation. Funnily enough, this project is built on top of Nick Thissen's .NET [iRacing SDKWrapper](https://github.com/NickThissen/iRacingSdkWrapper). I'm sure there are implementations for other environments that I am unaware of.

The technical side of the iRacing community is distributed across many niches. If you want to build, say, an Electron UI for iRacing, you'll have to learn the node.js library. If you want to build a separate .NET solution for some problem, you'll have to learn a .NET solution like. 

This project, although in very early development, aims to solve this problem by offering a single solution for all platforms and tech-stacks. You could use this to build a windows form project in .NET, or an overlay in Electron.js as I have. Use it in a python project to do some data analysis on your races. Learn one library and use it everywhere. 

Another significant motivation is that I like to build stuff and I like to learn. Building this and extending it as I need more data to build my own UI project is fun. It is unlikely that this will be a comprable solution to any of the above-mentioned projects any time soon.

## How it works

It reserves a network port on your machine and accepts web socket connection reqeusts. Once connected, the SDKWrapper-Service will send data via the web socket to be used in whatever way the client program wishes.

To interface with the iRacing SDK, it makes use of the above-mentioned SDKWrapper from Nick Thissen. The base iRacing SDK is written in C and C++, but Nick's wrapper is graciously written in C#, a programming language for actual humans.

There are a few services that handle the different responsibilities of the program. Excepting the UserPreferencesService, they are all event driven.
- SDKService
    - interfaces with the iRacingSDKWrapper to manage the iRacing SDK isntance. Gets telemetry data, emits events when updates happen.
- WebSocketService
    - manages the web socket connection(s) and sends the data from the SDKService to the client
- StartupService
    - handles events from all services and passes data from SDKService to the WebSocketService. This ensures a solid decoupling of responsibilities between the two.
    - Handles queueing of important events to be sent when the client is connected. For example, the first time the user gets on track, or when the user enters the garage. 
- UserPrefrencesService
    - manages a userPreferences.json file in the AppData directory. creates the file if it doesn't exist yet, loads it when it does exist, and updates the values when they are changed.
    - acts as a shared state from which all other services in the program should use to access user preferences

## Updates
The app uses [Velopack](https://github.com/velopack/velopack), (built off a fork of [squirrel.Windows](https://github.com/Squirrel/Squirrel.Windows)) to automatically download and install updates.

To simplify development this process is currently non-optional and non-interactive. If an update exists, it will install itself on program start. You will get a popup notifying you that the update is downloading, but there is no way to run the program if you stop this update process. If this is a concern to you, inspect the update code yourself in [Program.cs](iRacing SDKWrapperService/Program.cs). The releases are built from the source code in the repo and download from the [Releases]() page in this repo. It is defaulted to always install the latest non-pre-release release. 

If you do not want the automatic updates and new features as I add them, you can clone the repo, remove the update code from Program.cs, and build your own executable.

## Roadmap

While this is mainly a fun side-project for learning, I do plan to update it (in a probably irregular fashion) as I need new features to support my open-overlay UI. Below are my current priorities roughly in order from top to bottom.

- data for displaying standings
- data for displaying relative position and deltas

I am of course open to feedback or suggestions, and can be reached at <simple.overlay.dev@gmail.com>
