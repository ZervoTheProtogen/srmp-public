using Epic.OnlineServices;
using Epic.OnlineServices.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.EpicSDK
{
    public class EpicMetrics
    {
        private MetricsInterface metricsInterface;

        public EpicMetrics(MetricsInterface metricsInterface) 
        { 
            this.metricsInterface = metricsInterface;
        }

        public void BeginSession(Utf8String sessionId = null)
        {
            var beginPlayerSessionOptions = new BeginPlayerSessionOptions()
            {
                AccountId = SRSingleton<EpicApplication>.Instance.Authentication.ProductUserId.ToString(),
                DisplayName = SRSingleton<EpicApplication>.Instance.Authentication.Username,
                ControllerType = UserControllerType.Unknown,
                GameSessionId = sessionId,
                ServerIp = null
            };
            var result = metricsInterface.BeginPlayerSession(ref  beginPlayerSessionOptions);
            if(result != Epic.OnlineServices.Result.Success)
            {
                SRMultiplayer.SRMP.Log($"Failed to begin session: {result}");
            }
        }

        public void EndSession()
        {
            var endPlayerSessionOptions = new EndPlayerSessionOptions()
            {
                AccountId = SRSingleton<EpicApplication>.Instance.Authentication.ProductUserId.ToString()
            };
            var result = metricsInterface.EndPlayerSession(ref endPlayerSessionOptions);
            if (result != Epic.OnlineServices.Result.Success)
            {
                SRMultiplayer.SRMP.Log($"Failed to end session: {result}");
            }
        }
    }
}
