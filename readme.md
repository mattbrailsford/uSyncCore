uSync Three
===========

> uSync Three is in a very early build stage - it's not for use by the faint hearted - bits of it don't even work - if you are looking for proper more stable syncing stick with [uSync 2](http://our.umbraco.org/projects/developer-tools/usync) for now. It works  

uSync is the popular? syncing tool for umbraco, it gets all the bits of an umbraco installation into one place (the disk), this lets you control your umbraco installation they way you want to, you can source control Umbraco, copy it about using xcopy, or even send bits of it via email to people who won't have any clue what you are doing. 

uSync.Core
----------

uSync 3 is a ground up cleansing of uSync - over time usync has grown in complexity, as little things got added or random behaviours in specific versions of umbraco where addresses, so it's a bit messy. 

Also uSync was a proof of concept, and as a concept it works, but the code closely ties the usync processes of reading and writing from the disk, with the umbraco ones of importing and exporting information. 

uSync core is the tidy loosely coupled version, it's also the could do loads more version.

uSync Core - is all the import and export logic, from both usync and usync.content edition, that means the core is responsible for getting the database bits of usync in and out of XML. it also means that the core can be used for more than just syncing to disk.

> It's true that Umbraco has internal serialization calls, but they are built around the Packaging services. which means they are built to work with packages (which do things like not overwriting existing values. uSync overwrites values - it also do things like ID mapping. which means its a bit more complex than the core synchronization.

The Core will have an API and will let you use it in your own projects to pull data in and out of umbraco as you see fit - this opens up a world of new syncing possibilities - An early version of the core is in the [LocalGovStarterKit](http://our.umbraco.org/projects/starter-kits/localgov-starter-kit), for pulling in the example content when the package is imported. 

uSync.BackOffice
-----------------

uSync.Backoffice is uSync - it's the main syncing of all the back office things like doctypes, datatypes etc - it's just called back office inside usync 3 because we now have the core. 

The functionality of uSync.Backoffice will be identical to the previous versions of uSync - except it will have more flexibility so will allow us to more easily build things like comparison tools, and pre-post installation logging. 

uSync.Content
-------------

uSync.Content will be uSync content edition using the core. again just like the current uSync.ContentEdition it will import and export media and content from umbraco, and like uSync.Backoffice this version will allow us to develop more fancy logic around just what is and isn't going to change.

uSync.FutureEditions
---------------------

splitting usync and generating the core opens up a world of possibilities, some of which we are quite excited about - once we have usync 3 working just like the current uSync we will be exploring these new paths...

**uSync.Migrations** : instead of syncing all the time, migrations will allow you to take in time snapshots and build the changes up, so you can deploy a migration to a server and it will add just what you want and just like Entityframework we are going to see if we can do all this from the package manager console in visual studio

**uSync Packager** : pick a load of things inside umbraco and get a usync package to deploy to another server - it's a bit like the package tools already in umbraco - but usync packages are more aggressive, and we want to build in some version control. 

**uSync Nuget Helper** *especially if migrations works* wouldn't it be great if you could include things like doctypes, datatypes and even content inside an nuget package ? there is no reason why you can't add the files now - there is just no nice way of getting the items then imported into umbraco - the nuget helper will hopefully work at this by running as part of the nuget installation to put content and other stuff in to umbraco.

**uSync Package Helper** this one sort of exists already in the [LocalGovStarter kit](http://our.umbraco.org/projects/starter-kits/localgov-starter-kit) - an early version of the core is used to read in a load of example content (if the user wants it) - the installer reads in one big file of content and feeds it into the usync.core, this in a nicer way might help with all those niggly bits of package creation we all suffer from time to time.

**uSync many more?** these are just the things we're thinking about now while we are busy building the core. I am sure not all of these will happen and more  ideas will come that might. but for now it's all experimental. so keep in touch.
  

 