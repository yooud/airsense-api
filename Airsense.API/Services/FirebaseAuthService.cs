using FirebaseAdmin.Auth;

namespace Airsense.API.Services;

public class FirebaseAuthService : IAuthService
{
    public async Task<bool> SetIdAsync(string uid, int id)
    {
        try
        {
            var user = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);

            if (user is null)
                return false;
        
            var newClaims = user.CustomClaims.ToDictionary(claim => claim.Key, claim => claim.Value);
            newClaims["id"] = id;
        
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(uid, newClaims);
            return true;
        }
        catch (FirebaseAuthException ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }
}