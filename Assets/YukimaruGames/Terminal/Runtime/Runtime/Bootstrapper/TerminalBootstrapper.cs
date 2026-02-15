using UnityEngine;
using YukimaruGames.Terminal.Runtime.Shared;

namespace YukimaruGames.Terminal.Runtime
{
    public sealed partial class TerminalBootstrapper : MonoBehaviour
    {
        [Header("Installer")]
        [SerializeReference, SerializeInterface] 
        private ITerminalInstaller _installer = new TerminalStandardInstaller();

        private TerminalRuntimeScope _scope;

        private void Awake()
        {
            if (_installer == null)
            {
                _installer = new TerminalStandardInstaller();
            }
            
            _scope = _installer.Install();
        }

        private void Update()
        {
            _scope?.EntryPoint.Update();
        }

        private void OnGUI()
        {
            _scope?.EntryPoint.OnGUI();
        }

        private void OnDestroy()
        {
            if (_scope != null)
            {
                _installer?.Uninstall(_scope);
                _scope = null;
            }
        }
    }
}
