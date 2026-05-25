using NUnit.Framework;
using Keyvora.Desktop.Events;
using Keyvora.Desktop.Actions;
using Keyvora.Desktop.Actions.BuiltIn;
using Keyvora.Desktop.Profiles;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Keyvora.Desktop.Tests;

[TestFixture]
public class EventBusTests
{
    [Test]
    public void Publish_NotifiesSubscribers()
    {
        var bus = new EventBus();
        var fired = false;

        using (bus.Subscribe<ButtonPressedEvent>(e => fired = true))
        {
            bus.Publish(new ButtonPressedEvent(1));
            Assert.That(fired, Is.True);
        }
    }

    [Test]
    public void Unsubscribe_StopsNotifications()
    {
        var bus = new EventBus();
        var count = 0;

        var sub = bus.Subscribe<ButtonPressedEvent>(e => count++);
        sub.Dispose();

        bus.Publish(new ButtonPressedEvent(1));
        Assert.That(count, Is.EqualTo(0));
    }
}

[TestFixture]
public class ProfileTests
{
    private string _testDir = null!;
    private ProfileManager _manager = null!;

    [SetUp]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _manager = new ProfileManager(_testDir);
    }

    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Test]
    public void LoadProfiles_CreatesDefaultProfile_WhenEmpty()
    {
        _manager.LoadProfiles();
        Assert.That(_manager.Profiles.Count, Is.EqualTo(1));
        Assert.That(_manager.ActiveProfile, Is.Not.Null);
        Assert.That(_manager.ActiveProfile!.Name, Is.EqualTo("Default"));
    }

    [Test]
    public void AddProfile_AddsToList()
    {
        _manager.LoadProfiles();
        var profile = _manager.AddProfile("Test");

        Assert.That(_manager.Profiles.Count, Is.EqualTo(2));
        Assert.That(profile.Name, Is.EqualTo("Test"));
    }

    [Test]
    public void ActivateProfile_SwitchesActive()
    {
        _manager.LoadProfiles();
        var profile = _manager.AddProfile("Test");
        _manager.ActivateProfile(profile.Id);

        Assert.That(_manager.ActiveProfile?.Id, Is.EqualTo(profile.Id));
    }
}

[TestFixture]
public class ActionRegistryTests
{
    [Test]
    public void Register_AddsAction()
    {
        var registry = new ActionRegistry();
        registry.Register(new KeyboardShortcutAction());

        var action = registry.Get("builtin.keyboard");
        Assert.That(action, Is.Not.Null);
        Assert.That(action!.DisplayName, Is.EqualTo("Keyboard Shortcut"));
    }

    [Test]
    public void Register_Duplicate_Throws()
    {
        var registry = new ActionRegistry();
        registry.Register(new KeyboardShortcutAction());

        Assert.That(() => registry.Register(new KeyboardShortcutAction()),
            Throws.InvalidOperationException);
    }

    [Test]
    public void GetAll_ReturnsAll()
    {
        var registry = new ActionRegistry();
        registry.Register(new KeyboardShortcutAction());
        registry.Register(new LaunchApplicationAction());

        Assert.That(registry.GetAll().Count, Is.EqualTo(2));
    }
}

[TestFixture]
public class SerialProtocolTests
{
    [Test]
    public void TryDecode_ValidMessage_ReturnsTrue()
    {
        var result = Hardware.SerialProtocol.TryDecode("BTN_3", out int index);
        Assert.That(result, Is.True);
        Assert.That(index, Is.EqualTo(3));
    }

    [Test]
    public void TryDecode_InvalidMessage_ReturnsFalse()
    {
        var result = Hardware.SerialProtocol.TryDecode("INVALID", out _);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryDecode_Empty_ReturnsFalse()
    {
        var result = Hardware.SerialProtocol.TryDecode("", out _);
        Assert.That(result, Is.False);
    }
}
