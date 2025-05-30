using System;
using System.Linq.Expressions;
using FluentNHibernate.Mapping;
using IOKode.OpinionatedFramework.ContractImplementations.NHibernate.Postgres.UserTypes.NodaTime;
using IOKode.OpinionatedFramework.Events;

namespace IOKode.OpinionatedFramework.ContractImplementations.NHibernate.Postgres.Mappings;

public class EventMap : ClassMap<Event>
{
    public EventMap()
    {
        Not.LazyLoad();
        Schema("opinionated_framework");
        Table("events");
        Id<Guid>(column: "id").GeneratedBy.GuidComb();
        Map(x => x.DispatchedAt).Column("dispatched_at").Nullable().CustomType<InstantUserType>();
        Map(PayloadPropertyExpression()).Column("payload").Access.Using<EventPayloadAccessor>().CustomType<JsonBType>();

        DiscriminateSubClassesOnColumn("event_type");
    }

    private Expression<Func<Event, object>> PayloadPropertyExpression()
    {
        var eventType = typeof(Event);
        var eventPayloadType = typeof(EventPayloadAccessor);

        var eventTypeParam = Expression.Parameter(eventType, "x");
        var eventPayloadTypeParam = Expression.Parameter(eventPayloadType, "x");
        Expression expression = Expression.PropertyOrField(eventPayloadTypeParam, nameof(EventPayloadAccessor.Payload));

        return (Expression<Func<Event, object>>) Expression.Lambda(typeof(Func<Event, object>), expression, eventTypeParam);
    }
}

public class EventSubclassMap<T> : SubclassMap<T> where T : Event
{
    public EventSubclassMap()
    {
        Not.LazyLoad();
        DiscriminatorValue(typeof(T).FullName);
    }
}