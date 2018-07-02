mergeInto( LibraryManager.library, {
  RemoveLoader: function() {
    console.log("Loading finished!");
    document.getElementById('loader').remove();
  },

  GetSetNumber: function() {
    var queryNumber;
    var params = new URLSearchParams(location.search.slice(1));
    queryNumber = parseInt( params.get('set') );
    if( isNaN(queryNumber) || queryNumber < 0 ) queryNumber = 0; 
    console.log("RAW SET PARAMETER: " + queryNumber );
    return queryNumber;
  },

  GetBarType: function() {
      var queryNumber;
      var params = new URLSearchParams(location.search.slice(1));
      queryNumber = parseInt( params.get('bar') );
      if( isNaN(queryNumber) || queryNumber < 0 ) queryNumber = -1;
      console.log("RAW BAR PARAMETER: " + queryNumber );
      return queryNumber;
  },
});
