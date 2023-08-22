# Window-Visibility-Detector
I needed to know whether I have to render a specific window, or save the resources for computing different things.<br>
But since there is no native Windows solution or any other third party program, I made my own program. It should be easy to use,
and you can look at the example for help.

## Info ##
- Takes the following cases into consideration:
  - The given window is not a window
  - The given window is not in bounds (across all displays)
  - The given window is minimized / iconified
  - The given window is partially or fully overlapped with other windows

- GetExtendedInfo() returns a string containing a small description of the exact status of the window (should be one of the above)
- It's not completely optimized, but it runs pretty fast
- Ignores transparent windows (all windows with the layered style)
- Should be pixel-perfect. If not, just tweak the tolerance constant and read the related article in the comment
