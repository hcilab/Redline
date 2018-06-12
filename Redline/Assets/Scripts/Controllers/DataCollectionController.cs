using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using UnityEngine;
using UnityEngine.Networking;
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

    public void GetConfig( string resource, WebCallback action )
    {
        var path = GetServerPath() + _configEndpoint + resource;
        var req = UnityWebRequest.Get( path );
        StartCoroutine( Download( req, action ) );
    }
    
    public void GetNumberOfLevels( WebCallback action )
    {
        GetConfig( "levelCount", action );
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

    private void InitDatafile( string fileName = "redline_data.csv" )
    {
        var headerString = Encoding.ASCII.GetBytes( 
            "time, counter, id, level, hp, bar_type, damage, score, proximity, active, fps, type\n" );
        _dataFile = Path.Combine( Application.streamingAssetsPath, Path.Combine( "data", fileName ) );
        if ( !File.Exists( _dataFile ) )
        {
            FileStream fs = File.Create( _dataFile );
            if ( fs.CanWrite )
            {
                fs.Write( headerString, 0, headerString.Length );
                fs.Close();
            }
        }
    }

    public void LogData( 
        float time
        , string counter
        , int sessionId
        , int trial
        , string level
        , string bar_type
        , double hitPoints
        , double damage
        , double score
        , double flamesNearBy
        , double averageIntensity
        , double activeFlames
        , double fps
        , DataType type = DataType.Atomic )
    {
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR
        if( !File.Exists( _dataFile ) )
        {
            InitDatafile();
        }
        
        var dataString = string.Format( "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}\n", 
            time, counter, sessionId, level, hitPoints, bar_type, damage,
            score, flamesNearBy, averageIntensity, activeFlames, fps, 
            Enum.GetName( typeof( DataType ), type ) );
        File.AppendAllText( _dataFile, dataString );
        #endif    
    
        WWWForm dataObj = new WWWForm();
        dataObj.AddField( "time", time.ToString());
        dataObj.AddField( "counter", counter );
        dataObj.AddField( "id", sessionId );
        dataObj.AddField( "trial", trial );
        dataObj.AddField( "level", level );
        dataObj.AddField( "hp", hitPoints.ToString() );
        dataObj.AddField( "bar", bar_type );
        dataObj.AddField( "damage", damage.ToString() );
        dataObj.AddField( "score", score.ToString() );
        dataObj.AddField( "proximity", flamesNearBy.ToString() );
        dataObj.AddField( "avg_intensity_in_proximity", averageIntensity.ToString() );
        dataObj.AddField( "active", activeFlames.ToString() );
        dataObj.AddField( "fps", fps.ToString() );
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
    
    public void SaveToConfig( string level, MonoBehaviour gameObject )
    {
        string json = JsonUtility.ToJson( gameObject, true );
		
        string path = Path.Combine( Application.streamingAssetsPath, level + ".json" );

        byte[] jsonAsBytes = Encoding.ASCII.GetBytes( json );
        File.WriteAllBytes( path, jsonAsBytes  );
    }
}