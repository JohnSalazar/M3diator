namespace M3diator;

/// <summary>
/// Core interface of Mediator, combining ISender and IPublisher.
/// Defines the mediator for sending requests and publishing notifications.
/// </summary>
public interface IMediator : ISender, IPublisher { }