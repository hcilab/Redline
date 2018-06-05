using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class DataCollectionController : MonoBehaviour
{
    public enum DataType { Atomic, Final }

    public delegate void WebCallback( string data );

    public delegate void ProgressUpdate( float progress );
    
    [SerializeField] private string _serverAddress;
    [SerializeField] private int _serverPort;
    [SerializeField] private String _atomicEndpoint;
    [SerializeField] private String _finalEndpoint;
    [SerializeField] private bool _sendRemote;
    private string _dataFile;
    private Queue<UnityWebRequest> _uploadBacklog;

    private void Awake()
    {
        FindObjectOfType<GameMaster>().LoadFromConfig( "data_service", this );
        Debug.Log( "Using " + _serverAddress + ":" + _serverPort  );
        _uploadBacklog = new Queue<UnityWebRequest>();
    }
    
    public void Submit( WWWForm dataObj, DataType dataType )
    {
        if ( _sendRemote )
        {
            StartCoroutine( Upload( dataObj, dataType ) );
        }
    }

    public void GetNewID( WebCallback cb ) 
    {
        String path = GetServerPath() + "/id";
        var req = UnityWebRequest.Get( path );
        StartCoroutine( Download( req, cb ) );
    }
    
    public void GetBarType( WebCallback cb )
    {
        String path = GetServerPath() + "/bar";
        var req = UnityWebRequest.Get( path );
        StartCoroutine( Download( req, cb ) );
    }
        
    IEnumerator Download( UnityWebRequest req, WebCallback cb )
    {
        req.Send();
        yield return new WaitUntil( () => req.isDone && req.downloadHandler.isDone );
        if ( req.isError ) LogNetworkError( req );
        else if ( req.isDone )
        {
            Debug.Log( "New ID recieved: " + req.downloadHandler.text );
            cb( req.downloadHandler.text );
        }
    }

    IEnumerator Upload( WWWForm dataObj, DataType dataType )
    {
        var path = GetServerPath();
        switch ( dataType )
        {
                case DataType.Atomic:
                    path += "/";
                    break;
                case DataType.Final:
                    path += "/final/";
                    break;
        }
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
        while ( _uploadBacklog.Count != 0 )
        {
            var webRequest = _uploadBacklog.Dequeue();
            yield return webRequest.Send();
            if ( webRequest.isError )
            {
                throw new Exception( "Litterally can't even upload..." );
            }
            if ( webRequest.isDone )
            {
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
            "time, counter, id, level, hp, bar_type, damage, score, proximity, active, fps\n" );
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
        
        var dataString = string.Format( "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}\n", 
            time, counter, sessionId, level, hitPoints, bar_type, damage,
            score, flamesNearBy, averageIntensity, activeFlames, fps );
        File.AppendAllText( _dataFile, dataString );
        #endif    
    
        WWWForm dataObj = new WWWForm();
        dataObj.AddField( "time", time.ToString());
        dataObj.AddField( "counter", counter );
        dataObj.AddField( "id", sessionId );
        dataObj.AddField( "level", level );
        dataObj.AddField( "hp", hitPoints.ToString() );
        dataObj.AddField( "bar", bar_type );
        dataObj.AddField( "damage", damage.ToString() );
        dataObj.AddField( "score", score.ToString() );
        dataObj.AddField( "proximity", flamesNearBy.ToString() );
        dataObj.AddField( "avg_intensity_in_proximity", averageIntensity.ToString() );
        dataObj.AddField( "active", activeFlames.ToString() );
        dataObj.AddField( "fps", fps.ToString() );
        Submit( dataObj, type );
    }
}