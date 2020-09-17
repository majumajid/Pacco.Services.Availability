using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Pacco.Services.Availability.Core.Events;
using Pacco.Services.Availability.Core.Exceptions;
using Pacco.Services.Availability.Core.ValueObjects;

namespace Pacco.Services.Availability.Core.Entities
{
    public class Resource : AggregateRoot
    {
        private ISet<string> _tags = new HashSet<string>();
        private ISet<Reservation> _reservations = new HashSet<Reservation>();
        
        public IEnumerable<string> Tags
        {
            get => _tags;
            private set => _tags = new HashSet<string>(value);
        }
        
        public IEnumerable<Reservation> Reservations
        {
            get => _reservations;
            private set => _reservations = new HashSet<Reservation>(value);
        }

        public Resource(Guid id, IEnumerable<string> tags, IEnumerable<Reservation> reservations = null,
            int version = 0)
        {
            ValidateTags(tags);
            Id = id;
            Tags = tags;
            Reservations = reservations ?? Enumerable.Empty<Reservation>();
            Version = version;
        }

        private static void ValidateTags(IEnumerable<string> tags)
        {
            if (tags is null || !tags.Any())
            {
                throw new MissingResourceTagsException();
            }

            if (tags.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidResourceTagsException();
            }
        }

        public static Resource Create(Guid id, IEnumerable<string> tags, IEnumerable<Reservation> reservations = null)
        {
            var resource = new Resource(id, tags, reservations);
            resource.AddEvent(new ResourceCreated(resource));
            return resource;
        }

        public void AddReservation(Reservation reservation)
        {
            Version++;

            var hasCollidingReservation = _reservations.Any(HasTheSameReservationDate);

            if (hasCollidingReservation)
            {
                var collidingReservation = _reservations.First(HasTheSameReservationDate);
                if (collidingReservation.Priority >= reservation.Priority)
                {
                    throw new CannotExpropriateReservationException(Id, reservation.DateTime.Date);
                }

                _reservations.Remove(collidingReservation);
            }

            _reservations.Add(reservation);
            
            bool HasTheSameReservationDate(Reservation r) 
                => r.DateTime.Date == reservation.DateTime.Date;
        }

        public void ReleaseReservation(Reservation reservation)
        {
        }

        public void Delete()
        {
        }
    }
}