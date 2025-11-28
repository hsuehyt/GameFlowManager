# GameFlowManager

Simple scene flow controller for Unity projects that need fade-in / fade-out transitions between scenes.
Designed for immersive projection workflows where the “fader” is not just a UI image, but can be a custom-shaped blackout mesh (for projection-mapped walls).

This repository contains:

* GameFlowManager.cs – central scene and fade controller (DontDestroyOnLoad)
* SceneAdvanceProxy.cs – tiny helper component to trigger scene changes from Timeline, buttons, etc.

## How it works

1. You have one “bootstrap” or “master” scene that contains:

   * a GameObject with GameFlowManager
   * optional global UI / fade canvas
   * optional blackout geometry (for projection walls)

2. When the project starts, GameFlowManager:

   * remembers the bootstrap scene name
   * optionally auto-loads the first content scene (Auto Boot)

3. When you call GameFlowManager.Next("Scene02"):

   * it fades to black (Fade Out)
   * loads the target scene additively if it is not loaded yet
   * makes that scene the active scene
   * unloads all other scenes except:

     * the bootstrap scene
     * “DontDestroyOnLoad”
   * fades back from black (Fade In)

## Inspector fields

## Fade

canvasFader (optional)

* Type: CanvasGroup
* A normal UI-based fade.
* Typically a full-screen black Image + CanvasGroup on a UI Canvas.
* If assigned, its alpha will be animated during fades.

meshFaderObject (optional)

* Type: GameObject
* A custom-shaped blackout object, for example your projection-mapped wall geometry “wallsBlack”.
* GameFlowManager finds all Renderer components in this object and its children and animates the alpha of their materials.
* Materials should use a transparent, unlit shader (for example URP Unlit with Surface Type = Transparent, Blending = Alpha).

fadeTime

* Duration of each fade in seconds.
* Example: 2.0 means fade-out takes 2 seconds, and fade-in takes 2 seconds.

fadeCurve

* Type: AnimationCurve
* Shapes how the fade progresses over time (ease in, ease out, smooth step, etc.).
* X axis: 0 → 1 over fadeTime.
* Y axis: interpolation amount between start and target alpha.
* Default is a linear 0–1 curve.

You can use either canvasFader, meshFaderObject, or both at the same time:

* If only canvasFader is set: pure UI fade.
* If only meshFaderObject is set: world-space blackout geometry fade.
* If both are set: they fade together using the same fadeTime and fadeCurve.

## Auto Boot (optional)

autoLoadOnStart

* If true, GameFlowManager automatically loads the first content scene on Start.

firstScene

* Name of the first scene to load (e.g. “SceneTitle” or “Scene01”).
* Must be added to Build Settings.

bootDelay

* Time in seconds to wait before auto-booting.
* Useful if you want to show a logo, splash, or “waiting” state in the bootstrap scene before loading the first scene.

## Public API

Next(string sceneName)

* Starts the scene change flow to the given scene name.
* This is the main method used by other scripts, Timeline signals, button OnClick, etc.

Boot(string sceneName)

* Alias for Next. Provided just for readability.

OnSceneChanged (static event)

* Signature: Action<string>
* Fired every time the active scene changes.
* The string argument is the new active scene name.
* Useful if you want other systems (UI, logging, etc.) to react to scene changes.

## SceneAdvanceProxy

A tiny helper component to make calling GameFlowManager from Timeline or UI buttons easy.

Fields:

nextScene

* Name of the scene to go to when Next() is called.

Usage:

* Add SceneAdvanceProxy to any GameObject in your scene.
* Set nextScene in the Inspector.
* Call SceneAdvanceProxy.Next() from:

  * Timeline Signal Receiver
  * Animation Event
  * UnityEvent / Button OnClick

SceneAdvanceProxy.Next() simply calls GameFlowManager.I.Next(nextScene).

## Fade timing and audio behaviour

This section explains exactly when things happen during a scene change, and what happens to audio.

Assume:

* fadeTime = 2.0
* Next("Scene02") is called (via SceneAdvanceProxy or directly)

Order of operations:

1. Time t = 0.0

   * Next() starts coroutine CoNext().
   * CoNext immediately calls: yield return Fade(1f);
   * This is the Fade Out.
   * The fade begins **immediately at the moment of the signal**.

2. Time t = 0.0 → t = 2.0 (fadeTime)

   * canvasFader.alpha moves from current alpha to 1.0 using fadeCurve.
   * meshFaderObject’s material alphas move from current value to 1.0 using fadeCurve.
   * Visually, the screen / projection gradually goes to black.
   * **Audio is not touched by GameFlowManager.**
   * Any AudioSources in the scenes continue to play normally during this period.

3. Around t = 2.0

   * Fade(1f) completes.

   * CoNext continues:

     * Loads the target scene additively (if not already loaded).
     * Sets it as the active scene.
     * Raises OnSceneChanged(targetSceneName).
     * Unloads all other scenes except:

       * the bootstrap/master scene
       * “DontDestroyOnLoad”

   * When a scene is unloaded, its AudioSources are destroyed:

     * Any audio playing from those scenes will stop at this moment.
     * If some audio sources live in a DontDestroyOnLoad object, they will keep playing (by design).

4. After unloading, CoNext calls: yield return Fade(0f);

   * Fade In starts immediately after switching and unloading.
   * Over another fadeTime seconds (e.g. 2.0s), canvasFader and meshFaderObject fade from alpha 1.0 back to 0.0 using the fadeCurve.
   * The new scene gradually appears from black.

So:

* The fade starts **right when Next() / Boot() is triggered**.
* The old scene is unloaded **after the fade-out finishes** (after fadeTime seconds).
* Audio is **not faded** by this script:

  * It continues playing during the fade-out.
  * It stops only when its scene is unloaded (unless the audio object is in DontDestroyOnLoad).

## Audio notes

GameFlowManager does not change AudioSource volume, mixer levels, or any other audio property. It works purely on visual alpha (CanvasGroup and materials).

If you need audio fade-out or crossfades between scenes, you can:

* Add your own audio manager that:

  * listens to GameFlowManager.OnSceneChanged
  * or exposes a method you manually call before Next()
* Fade AudioSource volume or an AudioMixer exposed parameter in parallel to the visual fade.

## Typical setup examples

1. Classic UI Fade Only

* Have a full-screen UI Image with a CanvasGroup on a screen-space Canvas.
* Assign its CanvasGroup to canvasFader.
* Leave meshFaderObject empty.
* GameFlowManager will fade that UI image in and out.

2. Immersive projection blackout mesh only

* Create a custom-shaped blackout mesh “wallsBlack” that covers all projection-mapped surfaces.
* Use a transparent unlit black material.
* Put wallsBlack in your bootstrap/master scene.
* Assign wallsBlack to meshFaderObject.
* Leave canvasFader empty or use it only for debug desktop UI.
* GameFlowManager will fade your physical projection walls to black and back.

3. Combined UI + World Fade

* Assign both canvasFader and meshFaderObject.
* Both will fade together with the same fadeTime and fadeCurve.
* Useful if you want both a control-room UI overlay and immersive blackout.

## Installation

1. Copy GameFlowManager.cs and SceneAdvanceProxy.cs into your project.
2. Create a bootstrap scene (e.g. “SceneMaster”) and add it to Build Settings.
3. Add a GameObject and attach GameFlowManager.
4. Set:

   * canvasFader (optional)
   * meshFaderObject (optional)
   * fadeTime
   * fadeCurve
   * autoLoadOnStart, firstScene, bootDelay (as needed)
5. In your content scenes, add SceneAdvanceProxy objects and hook their Next() to signals or buttons to trigger scene changes.
