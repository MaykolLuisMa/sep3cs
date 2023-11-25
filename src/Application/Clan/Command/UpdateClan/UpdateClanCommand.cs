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
using DataClash.Domain.ValueObjects;
using MediatR;

namespace DataClash.Application.Clans.Commands.UpdateClan
{
  [Authorize]
  public record UpdateClanCommand : IRequest
    {
      public long Id { get; init; }
      public string? Description { get; init; }
      public string? Name { get; init; }
      public Region? Region { get; init; }
      public long TotalTrophiesToEnter { get; init; }
      public long TotalTrophiesWonOnWar { get; init; }
      public ClanType Type { get; init; }
    }

  public class UpdateClanCommandHandler : IRequestHandler<UpdateClanCommand>
    {
      private readonly IApplicationDbContext _context;
      private readonly ICurrentPlayerService _currentPlayer;
      private readonly ICurrentUserService _currentUser;
      private readonly IIdentityService _identityService;

      public UpdateClanCommandHandler (IApplicationDbContext context, ICurrentPlayerService currentPlayer, ICurrentUserService currentUser, IIdentityService identityService)
        {
          _context = context;
          _currentPlayer = currentPlayer;
          _currentUser = currentUser;
          _identityService = identityService;
        }

      public async Task Handle (UpdateClanCommand request, CancellationToken cancellationToken)
        {
          var userId = _currentUser.UserId!;
          var entity = await _context.Clans.FindAsync (new object[] { request.Id }, cancellationToken) ?? throw new NotFoundException (nameof (Clan), request.Id);
          var playerId = _currentPlayer.PlayerId!;
          var playerClan = await _context.PlayerClans.FindAsync (new object[] { request.Id, playerId }, cancellationToken);

          if (playerClan?.Role != Domain.Enums.ClanRole.Chief || await _identityService.IsInRoleAsync (userId, Roles.Administrator))
            throw new ForbiddenAccessException ();
          else
            {
              entity.Description = request.Description;
              entity.Name = request.Name;
              entity.Region = request.Region;
              entity.TotalTrophiesToEnter = request.TotalTrophiesToEnter;
              entity.TotalTrophiesWonOnWar = request.TotalTrophiesWonOnWar;
              entity.Type = request.Type;

              entity.AddDomainEvent (new ClanUpdatedEvent (entity));
              await _context.SaveChangesAsync (cancellationToken);
            }
        }
    }
}
