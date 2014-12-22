WPFAppBar 
=========

Available via Nuget : [https://www.nuget.org/packages/WpfAppBar/1.1.0](https://www.nuget.org/packages/WpfAppBar/1.1.0)

As seen in this StackOverflow question:

[How do you do AppBar docking (to screen edge, like WinAmp) in WPF?](http://stackoverflow.com/q/75785/12643)


What is it?
----------
A helper for turning a WPF window into an "AppBar" like the windows taskbar.  
I hope you're not writing any applications that need to do this, but if you 
are, hopefully this library will help.


How do I use it?
----------------
To use, just call this code from anywhere within a normal wpf window (say a button click or the initialize). Note that you can not call this until AFTER the window is initialized, if the HWND hasn't been created yet (like in the constructor), an error will occur.


```C#
//Make the window an appbar:
AppBarFunctions.SetAppBar( this, ABEdge.Right );
  
//Restore the window to a normal window:
AppBarFunctions.SetAppBar( this, ABEdge.None );
```

I found a bug!
--------------
Please add an issue, or better yet send a pull request. 
Thanks!



That sounds okay... but licensing?
----------------------------------

No warantee of any kind implied.

Creative Commons CC0 (https://creativecommons.org/publicdomain/zero/1.0/)

