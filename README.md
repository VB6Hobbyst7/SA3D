# SA3D - A new modern Sonic Adventure 3D modding tool
SA3D started with the idea of making a cleaner, more user friendly version of SAMDL and SALVL from SATools. <br/>
It started out with a rewritten, fully documented SAModel library, which grew to host a graphics library that can be used by our Modding tools and other Developers, to potentially create entire games. <br/>

As of now, SA3D is capable of loading all Sonic adventure model formats, including animations- but that's about it. <br/>
We are working on making it a fully fledged editor, that should at least be able to replace SAMDL and SALVL. <br/>

## Installation
The program was released for 64x only (at the moment), and thus requires the .net 5 64x runtime to be installed. The link to the runtime can be found [here](https://dotnet.microsoft.com/download)

## Usage
You can either simply run the exe, or drag and drop an lvl or mdl file onto it. <br/>
The program also has several startup parameters, with which it can be run in a "standalone" (aka 3d viewing only) mode, or with which a motion file can be attached. <br/>
To see all startup parameters, run SA3D in a terminal with the "--help" or "?" argument <br/>

## Plans
This is all for now. Unfortunately, textures and alike arent supported right now, but that will come in the future. <br/>
SA3D was intended to be part of SATools before, but due to too many changes in the libraries used, it was moved to its own repository and solution. Most of the program was done from scratch, so that the code used was as clean as possible. <br/>
Right now the goal is to have it support all functionality as SAMDL and SALVL did. <br/>
