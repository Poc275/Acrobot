using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;

namespace Acrobot.Models
{
    public enum LocationOptions
    {
        Derby = 1,
        Bristol,
        Dahlewitz,
        Indianapolis,
        Bangalore,
        Houston,
        Wellington
    }

    public enum AmenitiesOptions
    {
        AudioConferencing = 1,
        NetworkAccess,
        Whiteboard,
        Projector
    }

    [Serializable]
    public class RoomBooking
    {
        public LocationOptions MeetingLocation;
        public DateTime MeetingTime;
        public double NumberOfHours;
        public int NumberOfAttendees;
        public List<AmenitiesOptions> Amenities;

        public static IForm<RoomBooking> BuildForm()
        {
            return new FormBuilder<RoomBooking>()
                .Message("Welcome to the room booking bot!")
                .Build();
        }
    }
}