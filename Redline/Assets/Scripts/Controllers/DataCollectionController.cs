using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class DataCollectionController : MonoBehaviour
{
    public enum DataType { Atomic, Death, Timeout, Victory }
    
    public delegate void WebCallback( string data );

    public delegate void ProgressUpdate( float progress );
    
    [SerializeField] private string _serverAddress = "localhost";
    [SerializeField] private string _serverEndpoint = "/redline";
    [SerializeField] private int _serverPort = 80;
    [SerializeField] private bool _sendRemote = true;
    [SerializeField] private string _idEndpoint = "/id";
    [SerializeField] private string _atomicEndpoint = "/";
    [SerializeField] private string _finalEndpoint = "/final/";
    [SerializeField] private string _barEndpoint = "/bar";
    [SerializeField] private string _configEndpoint = "/config/";
    [SerializeField] private string _validationEndpoint = "/invalidate/";
    [SerializeField] private string _trialEndpoint = "/trial/";
    private string _dataFile;
    private Queue<UnityWebRequest> _uploadBacklog;
    private string[] sets;
    private int setNumber;

    [DllImport( "__Internal" )]
    private static extern string GetHostAddress();
    
    private void Awake()
    {
        Debug.Log( "Using " + _serverAddress + ":" + _serverPort  );
        _uploadBacklog = new Queue<UnityWebRequest>();
        #if UNITY_WEBGL && !UNITY_EDITOR
            _serverAddress = GetHostAddress();
        #endif
        if ( _serverEndpoint != "" ) {
            _serverAddress += _serverEndpoint;
        }
    }
    
    public void GetNewID( WebCallback cb ) 
    {
        String path = GetServerPath() + _idEndpoint;
        var req = UnityWebRequest.Get( path );
        StartCoroutine( Download( req, cb ) );
    }

    public void GetTrial( int id, WebCallback cb )
    {
        string path = GetServerPath() + _trialEndpoint + id;
        var req = UnityWebRequest.Get( path );
        StartCoroutine( Download( req, cb ) );
    }
    
    public void GetBarType( WebCallback cb )
    {
        String path = GetServerPath() + _barEndpoint;
        var req = UnityWebRequest.Get( path );
        StartCoroutine( Download( req, cb ) );
    }

    public void GetConfig( string lvl, string set, WebCallback action )
    {
        GetConfig( lvl + "?set=" + set, action );
    }

    public void GetConfig( string resource, WebCallback action )
    {
        var path = GetServerPath() + _configEndpoint + resource;
        var req = UnityWebRequest.Get( path );
        Debug.Log(path);
        StartCoroutine( Download( req, action ) );
    }
    
    public void GetNumberOfLevels( WebCallback action, string set = null )
    {
        string req = "levelCount";
        if ( set != null ) req += "?set=" + set;
        GetConfig( req, action );
    }

    public void InvalidateTrial( int session, int trial )
    {
        var path = GetServerPath() + _validationEndpoint;
        WWWForm dataObj = new WWWForm();
        dataObj.AddField( "id", session.ToString() );
        dataObj.AddField( "trial", trial.ToString() );
        StartCoroutine( Upload( dataObj, path ) );
    }

    [Serializable]
    public class StringArrayWrapper { public string items; }

    IEnumerator Download( UnityWebRequest req, WebCallback cb )
    {
        if (!_sendRemote)
        {
            //HTTPS://
            string parsedURL = req.url.Substring(req.url.LastIndexOf(_serverPort.ToString())).Substring(req.url.IndexOf("/"));
            string partialURL = parsedURL;
            if (parsedURL.IndexOf("/") > 0) {
                partialURL = parsedURL.Substring(0, parsedURL.IndexOf("/"));
            }
            string body = "";
            switch (partialURL)
            {
                case "id":
                    int id = GenerateID();
                    body = "{\"id\":0" + id.ToString() + "}";
                    break;
                case "bar":
                    body = "{\"bar\":0}";
                    break;
                case "trial":
                    //TODO
                    body = "{\"trial\":0}";
                    break;
                case "config":
                    var newString = parsedURL.Substring(parsedURL.IndexOf("/") + 1);
                    if (newString == "player")
                    {
                        string playerData = Resources.Load<TextAsset>("Data/player").text;
                        body = playerData;
                        break;
                    }
                    else if (newString.StartsWith("levelCount"))
                    {
                        //Reduce set number by 1 to test on training set
                        setNumber = int.Parse(newString.Substring(newString.IndexOf("=") + 1));
                        string setData = Resources.Load<TextAsset>("Data/sets").text;
                        setData = setData.Substring(setData.IndexOf("[")+1, setData.LastIndexOf("]") - setData.IndexOf("[")-1).Replace(" ","").Replace("\n", "");
                        sets = Regex.Split(setData, @"\],.?\[");
                        string[] separateSets = sets[setNumber].Split(',');
                        body = "{\"count\":" + separateSets.Length.ToString() + "}";
                        break;
                    }
                    else if (newString.Contains("set="))
                    {
                        //Reduce set number by 1 to test on training set
                        int trialNumber = int.Parse(Regex.Match(newString, "^[0-9]*").Value)-1;
                        int levelNumber = int.Parse(sets[setNumber].Split(',')[trialNumber]);
                        string levelData = Resources.Load<TextAsset>("Data/levels").text;
                        string jsonString = JsonHelper.fixJson(levelData);
                        LevelData[] items = JsonHelper.FromJson<LevelData>(jsonString);
                        body = ConstructLevelBody(items[levelNumber], Resources.Load<TextAsset>("Data/defaultLevel").text);
                        Debug.Log(body);
                    }
                    break;
                default:
                    break;
            }
            cb(body);
        }
        else {
            Debug.Log("GET " + req.url);
            req.chunkedTransfer = false;
            req.Send();
            yield return new WaitUntil(() => req.isDone && req.downloadHandler.isDone);
            if (req.isNetworkError) LogNetworkError(req);
            else if (req.isDone)
            {
                Debug.Log("New data downloaded: " + req.downloadHandler.text);
                cb(req.downloadHandler.text);
            }
        }
    }

    IEnumerator Upload( WWWForm dataObj, string path )
    {
        if (!_sendRemote) {
            yield break;
        }

        UnityWebRequest req = UnityWebRequest.Post( path, dataObj );
        Debug.Log("POST " + req.url );
        Debug.Log( dataObj.ToString() );
        yield return req.Send();

        if ( req.isNetworkError )
        {
            LogNetworkError( req );
            _uploadBacklog.Enqueue( req );
        } else if ( req.isDone )
        {
            Debug.Log("Data upload complete.");
        }
    }

    public IEnumerator ProcessUploadBacklog( ProgressUpdate progressUpdate )
    {
        float totalBacklog = _uploadBacklog.Count;
        Debug.Log("We have " + totalBacklog + " items to upload"  );
        if ( Math.Abs( totalBacklog ) < 1f || !_sendRemote) progressUpdate( 1 );
        for ( int i = 0; i < totalBacklog; i++ )
        {
            var webRequest = _uploadBacklog.Peek();
            yield return webRequest.Send();
            if ( webRequest.isDone )
            {
                _uploadBacklog.Dequeue();
                progressUpdate( 1f - _uploadBacklog.Count / totalBacklog );
            }
        }
    }

    private static void LogNetworkError( UnityWebRequest req )
    {
        Debug.LogError( "Networking error: " + req.error );
        Debug.LogError( "Networking response: " + req.responseCode );
    }

    private string GetServerPath()
    {
//      Disabling the port for the heroku setup
        String path = "http://" + _serverAddress + ":" + _serverPort;
//      String path = "http://" + _serverAddress;
        return path;
    }

    public void LogData( 
        float time
        , string counter
        , string mturk
        , string sessionId
        , string trial
        , string level
        , string setNumber
        , string bar_type
        , string hitPoints
        , string damage
        , string score
        , string flamesNearBy
        , string averageIntensity
        , string activeFlames
        , string distanceTravelled
        , string waterUsed
        , string fps
        , DataType type = DataType.Atomic )
    {
    
        WWWForm dataObj = new WWWForm();
        dataObj.AddField( "time", time.ToString());
        dataObj.AddField( "counter", counter );
        dataObj.AddField( "mturk_id", mturk );
        dataObj.AddField( "id", sessionId );
        dataObj.AddField( "trial", trial );
        dataObj.AddField( "level", level );
        dataObj.AddField( "set", setNumber );
        dataObj.AddField( "hp", hitPoints );
        dataObj.AddField( "bar", bar_type );
        dataObj.AddField( "damage", damage );
        dataObj.AddField( "score", score );
        dataObj.AddField( "proximity", flamesNearBy );
        dataObj.AddField( "avg_intensity_in_proximity", averageIntensity );
        dataObj.AddField( "active", activeFlames );
        dataObj.AddField( "distance", distanceTravelled);
        dataObj.AddField( "waterUsed", waterUsed);
        dataObj.AddField( "fps", fps );
        dataObj.AddField( "type", Enum.GetName( typeof( DataType ), type ) );
        var path = GetServerPath();
        switch ( type )
        {
            case DataType.Atomic:
                path += _atomicEndpoint;
                break;
            case DataType.Death: 
            case DataType.Timeout: 
            case DataType.Victory:
                path += _finalEndpoint;
                break;
        }

        if ( type == DataType.Atomic )
        {
            var r = UnityWebRequest.Post( path, dataObj );
            _uploadBacklog.Enqueue( r );
            return;
        }
        
        StartCoroutine( Upload( dataObj, path ) );
    }

    string ConstructLevelBody(LevelData ld, string defaultLevel)
    {
        string ret = "{";
        for(int i = 0; i < ld._startingFlames.Count; i++)
        {
            ret += "\"_startingFlames\":[{\"x\":" + ld._startingFlames[i].x.ToString() +
                ",\"y\":" + ld._startingFlames[i].y.ToString() + "}],";
    }
        defaultLevel = defaultLevel.Substring(defaultLevel.IndexOf("{") + 1, defaultLevel.LastIndexOf("}") - defaultLevel.IndexOf("{"))
            .Replace("\n", "").Replace(" ", "");
        defaultLevel = Regex.Replace(defaultLevel, ",\"_prewarm\":[0-9]+", "");
        ret += "\"_prewarm\":" + ld._prewarm + "," + defaultLevel;
        return ret;
    }

    int GenerateID()
    {
        return UnityEngine.Random.Range(1, 100000);
    }
}

[Serializable]
public class StartingFlameData {    
    public double x;
    public double y;
}
[Serializable]
public class LevelData
{
    public List<StartingFlameData> _startingFlames;
    public int _prewarm;
}