using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    [SerializeField] private int _serverPort = 9500;
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

    private void Awake()
    {
        Debug.Log( "Using " + _serverAddress + ":" + _serverPort  );
        _uploadBacklog = new Queue<UnityWebRequest>();
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
        
    IEnumerator Download( UnityWebRequest req, WebCallback cb )
    {
        Debug.Log("GET " + req.url  );
        req.chunkedTransfer = false;
        req.Send();
        yield return new WaitUntil( () => req.isDone && req.downloadHandler.isDone );
        if ( req.isError ) LogNetworkError( req );
        else if ( req.isDone )
        {
            Debug.Log( "New data downloaded: " + req.downloadHandler.text );
            cb( req.downloadHandler.text );
        }
    }

    IEnumerator Upload( WWWForm dataObj, string path )
    {
        UnityWebRequest req = UnityWebRequest.Post( path, dataObj );
        Debug.Log("POST " + req.url );
        Debug.Log( dataObj.ToString() );
        yield return req.Send();

        if ( req.isError )
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
        var totalBacklog = _uploadBacklog.Count;
        if ( totalBacklog == 0 ) progressUpdate( 1 );
        for ( int i = 0; i < _uploadBacklog.Count; i++ )
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
        String path = "http://" + _serverAddress + ":" + _serverPort;
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
}