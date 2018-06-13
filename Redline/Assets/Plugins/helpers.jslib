mergeInto( LibraryManager.library, {
  RemoveLoader: function() {
    console.log("Loading finished!");
    document.getElementById('loader').remove();
  }
});
