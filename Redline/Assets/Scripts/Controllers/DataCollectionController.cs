using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class DataCollectionController : MonoBehaviour
{
    public enum DataType { Atomic, Final }
    
    [SerializeField] private string _serverAddress;
    [SerializeField] private int _serverPort;
    [SerializeField] private String _atomicEndpoint;
    [SerializeField] private String _finalEndpoint;
    [SerializeField] private bool _sendRemote;
    private string _dataFile;

    private void Awake()
    {
        FindObjectOfType<GameMaster>().LoadFromConfig( "data_service", this );
        Debug.Log( "Using " + _serverAddress + ":" + _serverPort  );
    }
    
    public void Submit( WWWForm dataObj, DataType dataType )
    {
        if ( _sendRemote )
        {
            StartCoroutine( Upload( dataObj, dataType ) );
        }
    }

    public int GetNewID()
    {
        String path = GetServerPath() + "/id";
        UnityWebRequest req = UnityWebRequest.Get( path );
        StartCoroutine( Download( req ) );
        do
        {} while ( !req.downloadHandler.isDone );
        var result = JsonUtility.FromJson<IdObject>( req.downloadHandler.text );
        Debug.Log( result  );
        return Int32.Parse( result.id );
    }

    IEnumerator Download( UnityWebRequest req )
    {
        yield return req.Send();
        if ( req.isError ) LogNetworkError( req );
        else if ( req.isDone ) Debug.Log( "New ID recieved: " + req.downloadHandler.text );
    }

    private class IdObject
    {
        public string id;
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
        } else if ( req.isDone )
        {
            Debug.Log("Data upload complete.");
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
            "time, counter, id, level, hp, bar_type, damage, score, proximity, active\n" );
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
        , DataType type = DataType.Atomic )
    {
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR
        if( !File.Exists( _dataFile ) )
        {
            InitDatafile();
        }
        
        var dataString = string.Format( "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}, {10}\n", 
            time, counter, sessionId, level, hitPoints, bar_type, damage,
            score, flamesNearBy, averageIntensity, activeFlames );
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
        Submit( dataObj, type );
    }
}