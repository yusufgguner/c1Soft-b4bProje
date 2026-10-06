using c1Soft_b4bProje.Areas.Admin;
using c1Soft_b4bProje.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace c1Soft_b4bProje.Hubs;

[Authorize(AuthenticationSchemes = AdminPanel.Sema, Roles = Roller.SistemAdmin)]
public class SiparisHub : Hub
{
}
