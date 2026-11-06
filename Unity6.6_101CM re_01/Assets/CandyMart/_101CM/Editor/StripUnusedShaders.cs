using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 빌드에서 Unity AI 추론 패키지(com.unity.ai.inference, Sentis) 셰이더를 뺀다.
/// 게임에서는 쓰지 않는데(에디터 AI 도구용) 패키지 Resources 폴더라 빌드에 딸려 와서
/// 컴파일 경고 수백 개와 용량만 늘린다. 셰이더 이름이 Hidden/Sentis/ 로 시작하거나
/// 패키지 경로 안의 컴퓨트 셰이더면 변형을 전부 지운다.
/// </summary>
class StripUnusedShaders : IPreprocessShaders, IPreprocessComputeShaders
{
    public int callbackOrder => 0;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (shader.name.StartsWith("Hidden/Sentis/")) data.Clear();
    }

    public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
    {
        string path = UnityEditor.AssetDatabase.GetAssetPath(shader);
        if (path.StartsWith("Packages/com.unity.ai.inference/")) data.Clear();
    }
}
