using UnityEditor;
using UnityEngine;

/// <summary>Resources/Sound 임포트 설정: BGM은 스트리밍(메모리 절약), 효과음은 미리 풀어두기(바로 재생).</summary>
public class GameAudioImport : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.Contains("/_101CM/Resources/Sound/")) return;
        var imp = (AudioImporter)assetImporter;
        bool isBgm = assetPath.Contains("/BGM_");
        var s = imp.defaultSampleSettings;
        s.loadType = isBgm ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = isBgm ? 0.6f : 0.8f;
        imp.defaultSampleSettings = s;
        imp.loadInBackground = isBgm;
    }
}
