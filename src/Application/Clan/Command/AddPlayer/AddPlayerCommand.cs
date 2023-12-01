/* Copyright (c) 2023-2025
 * This file is part of sep3cs.
 *
 * sep3cs is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * sep3cs is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with sep3cs. If not, see <http://www.gnu.org/licenses/>.
 */
using DataClash.Application.Common.Exceptions;
using DataClash.Application.Common.Interfaces;
using DataClash.Application.Common.Security;
using DataClash.Domain.Entities;
using DataClash.Domain.Enums;
using DataClash.Domain.Events;
using MediatR;

namespace DataClash.Application.Clans.Commands.AddPlayer
{
  [Authorize]
  public record AddPlayerCommand : IRequest
    {
      public long ClanId { get; init; }
      public long PlayerId { get; init; }
      public ClanRole Role { get; init; }
    }

  public class AddPlayerCommandHandler : IRequestHandler<AddPlayerCommand>
    {
      private readonly IApplicationDbContext _context;
      private readonly ICurrentPlayerService _currentPlayer;
      private readonly ICurrentUserService _currentUser;
      private readonly IIdentityService _identityService;

      public AddPlayerCommandHandler (IApplicationDbContext context, ICurrentPlayerService currentPlayer, ICurrentUserService currentUser, IIdentityService identityService)
        {
          _context = context;
          _currentPlayer = currentPlayer;
          _currentUser = currentUser;
          _identityService = identityService;
        }

      public async Task Handle (AddPlayerCommand request, CancellationToken cancellationToken)
        {
          var userId = _currentUser.UserId!;
          var clan = await _context.Clans.FindAsync (new object[] { request.ClanId }, cancellationToken) ?? throw new NotFoundException (nameof (Clan), request.ClanId);
          var playerId = _currentPlayer.PlayerId!;
          var playerClan = await _context.PlayerClans.FindAsync (new object[] { request.ClanId, playerId }, cancellationToken);

          if (playerClan?.Role != ClanRole.Chief && await _identityService.IsInRoleAsync (userId, Roles.Administrator) == false)
            throw new ForbiddenAccessException ();
          else if (_context.PlayerClans.Where (e => e.ClanId == clan.Id && e.Role == ClanRole.Chief).Any ())
            throw new ApplicationConstraintException ("Application constraint violation");
          else
            {
              var entity = new PlayerClan { ClanId = request.ClanId, PlayerId = request.PlayerId, Role = request.Role, };

              clan.AddDomainEvent (new PlayerAddedEvent<PlayerClan> (entity));
              _context.PlayerClans.Add (entity);

              await _context.SaveChangesAsync (cancellationToken);
            }
        }
    }
}