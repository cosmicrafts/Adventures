mergeInto(LibraryManager.library, {
  ResumeAudioContext: function() {
    try {
      if (typeof (window) !== "undefined" &&
          typeof (window.unityInstance) !== "undefined" &&
          typeof (window.unityInstance.Module) !== "undefined" &&
          typeof (window.unityInstance.Module.context) !== "undefined") {
        
        window.unityInstance.Module.context.resume().then(function() {
          console.log("AudioContext resumed successfully");
        }).catch(function(error) {
          console.error("Failed to resume AudioContext: ", error);
        });
      }
    } catch (error) {
      console.error("Error while trying to resume AudioContext: ", error);
    }
  }
}); 