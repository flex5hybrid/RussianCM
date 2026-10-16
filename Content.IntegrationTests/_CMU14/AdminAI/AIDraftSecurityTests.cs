using System;
using System.Linq;
using System.Text;
using Content.Server.CMU14.AdminAI;
using Content.Shared.Administration.AdminAI;
using NUnit.Framework;

namespace Content.IntegrationTests.CMU14.AdminAI;

[TestFixture]
public sealed class AIDraftSecurityTests
{
    private static readonly Guid Owner = Guid.NewGuid(), Player = Guid.NewGuid(), Other = Guid.NewGuid(), Selection = Guid.NewGuid();
    private static AIDraftSessionStore Store() => new(new());
    private static AIDraftSessionStore.View Open(AIDraftSessionStore store)
    {
        store.Accept(Player, 1, false, "Где находятся опубликованные правила сервера?", false, TimeSpan.Zero);
        return store.Open(Owner, Player, Selection, 1, TimeSpan.Zero, true)!;
    }

    [Test]
    public void PlayerOrUnknownChannelCannotOpenPrivateView()
    {
        var store = Store(); Open(store);
        store.Accept(Other, 1, true, "admin creates fake case", false, TimeSpan.Zero);
        Assert.That(store.Open(Other, Other, Selection, 1, TimeSpan.Zero, true), Is.Null);
        Assert.That(store.Open(Other, Player, Selection, 1, TimeSpan.Zero, false), Is.Null);
        Assert.That(store.Open(Other, Guid.NewGuid(), Selection, 1, TimeSpan.Zero, true), Is.Null);
        Assert.That(store.Open(Other, Player, Selection, 2, TimeSpan.Zero, true), Is.Null);
    }

    [Test]
    public void ForgedChannelOwnerOrViewCannotAccessAnotherDraft()
    {
        var store = Store(); var view = Open(store);
        Assert.That(store.Get(Other, Player, view.Id, Selection, 1, TimeSpan.Zero, true), Is.Null);
        Assert.That(store.Get(Owner, Other, view.Id, Selection, 1, TimeSpan.Zero, true), Is.Null);
        Assert.That(store.Get(Owner, Player, Guid.NewGuid(), Selection, 1, TimeSpan.Zero, true), Is.Null);
        Assert.That(store.Get(Owner, Player, view.Id, Guid.NewGuid(), 1, TimeSpan.Zero, true), Is.Null);
        Assert.That(store.Get(Owner, Player, view.Id, Selection, 1, TimeSpan.Zero, false), Is.Null);
    }

    [Test]
    public void AdminOnlyMessagesNeverBecomePromptContext()
    {
        var store = Store();
        store.Accept(Other, 1, true, "secret admin note", true, TimeSpan.Zero);
        Assert.That(store.Open(Owner, Other, Selection, 1, TimeSpan.Zero, true), Is.Null);
        var view = Open(store);
        store.Accept(Player, 1, true, "private accusation hidden note", true, TimeSpan.Zero);
        var ticket = store.Begin(view, TimeSpan.Zero, 1400)!;
        Assert.That(ticket.Conversation, Does.Not.Contain("private").And.Not.Contain("hidden"));
        Assert.That(ticket.Conversation, Does.Contain("опубликованные правила"));
    }

    [TestCase("close")]
    [TestCase("new-message")]
    [TestCase("permissions")]
    [TestCase("round")]
    [TestCase("selection")]
    [TestCase("disable")]
    public void ChangedContextDropsLateResult(string change)
    {
        var store = Store(); var view = Open(store); var ticket = store.Begin(view, TimeSpan.Zero, 1400)!;
        switch (change)
        {
            case "close": store.Revoke(Owner); break;
            case "new-message": store.Accept(Player, 1, false, "новое сообщение", false, TimeSpan.FromSeconds(1)); break;
            case "permissions": store.Revoke(Owner); break;
            case "round": store.Reset(); break;
            case "selection": store.Open(Owner, Player, Guid.NewGuid(), 1, TimeSpan.FromSeconds(1), true); break;
            case "disable": store.Reset(); break;
        }
        Assert.That(store.IsCurrent(ticket, 1, TimeSpan.FromSeconds(1), true), Is.False);
        Assert.That(store.Complete(ticket, 1, TimeSpan.FromSeconds(1), true, AIInferenceStatus.Success, "late private draft"), Is.Null);
    }

    [Test]
    public void ResultIsBoundToRoundPrivilegesAndOriginalCorrelation()
    {
        var store = Store(); var view = Open(store); var ticket = store.Begin(view, TimeSpan.Zero, 1400)!;
        Assert.That(store.Complete(ticket, 2, TimeSpan.Zero, true, AIInferenceStatus.Success, "bad"), Is.Null);
        Assert.That(store.Complete(ticket, 1, TimeSpan.Zero, false, AIInferenceStatus.Success, "bad"), Is.Null);
        Assert.That(store.Complete(ticket with { Id = Guid.NewGuid() }, 1, TimeSpan.Zero, true, AIInferenceStatus.Success, "bad"), Is.Null);
        Assert.That(store.Complete(ticket, 1, TimeSpan.Zero, true, AIInferenceStatus.Success, "review me"), Is.SameAs(view));
        Assert.That(view.Draft, Is.EqualTo("review me"));
    }

    [Test]
    public void ConversationsAndAdminsDoNotSharePromptOrResults()
    {
        var store = Store(); var first = Open(store);
        store.Accept(Other, 1, false, "other player exclusive text", false, TimeSpan.Zero);
        var second = store.Open(Guid.NewGuid(), Other, Guid.NewGuid(), 1, TimeSpan.Zero, true)!;
        var a = store.Begin(first, TimeSpan.Zero, 1400)!; var b = store.Begin(second, TimeSpan.Zero, 1400)!;
        Assert.That(a.Conversation, Does.Not.Contain("exclusive"));
        Assert.That(b.Conversation, Does.Contain("exclusive").And.Not.Contain("опубликованные"));
        store.Complete(a, 1, TimeSpan.Zero, true, AIInferenceStatus.Success, "first draft");
        Assert.That(second.Draft, Is.Empty);
    }

    [Test]
    public void CooldownSurvivesSwitchingBetweenChannelsAndOnlyOneRequestCanRunPerView()
    {
        var store = Store(); var view = Open(store);
        Assert.That(store.Begin(view, TimeSpan.Zero, 1400), Is.Not.Null);
        Assert.That(store.Begin(view, TimeSpan.FromSeconds(1), 1400), Is.Null);
        store.Open(Owner, Guid.NewGuid(), Guid.NewGuid(), 1, TimeSpan.FromSeconds(1), true);
        var reopened = store.Open(Owner, Player, Guid.NewGuid(), 1, TimeSpan.FromSeconds(1), true)!;
        Assert.That(store.Begin(reopened, TimeSpan.FromSeconds(2), 1400), Is.Null);
        Assert.That(store.Begin(reopened, TimeSpan.FromSeconds(5), 1400), Is.Not.Null);
    }

    [Test]
    public void ContextAndDraftTtlRemoveContentAndSources()
    {
        var store = Store(); var view = Open(store); var ticket = store.Begin(view, TimeSpan.Zero, 1400)!;
        store.Complete(ticket, 1, TimeSpan.Zero, true, AIInferenceStatus.Success, "private draft");
        view.Sources = [new("guide", "title", "hash", "fragment")];
        store.Sweep(TimeSpan.FromSeconds(120)); Assert.That(view.Draft, Is.Empty); Assert.That(view.Sources, Is.Empty);
        store.Sweep(TimeSpan.FromSeconds(300)); Assert.That(store.ConversationCount, Is.Zero); Assert.That(store.Owners, Is.Empty);
    }

    [Test]
    public void FailedResultNeverRetainsDraftText()
    {
        var store = Store(); var view = Open(store); var ticket = store.Begin(view, TimeSpan.Zero, 1400)!;
        store.Complete(ticket, 1, TimeSpan.Zero, true, AIInferenceStatus.InvalidResponse, "untrusted wrong draft");
        Assert.That(view.Draft, Is.Empty);
    }

    [Test]
    public void BoundedContextEvictionInvalidatesOutstandingRequest()
    {
        var store = new AIDraftSessionStore(new(MaxConversations: 1)); var view = Open(store);
        var ticket = store.Begin(view, TimeSpan.Zero, 1400)!;
        store.Accept(Other, 1, false, "next player", false, TimeSpan.FromSeconds(1));
        Assert.That(store.ConversationCount, Is.EqualTo(1));
        Assert.That(store.IsCurrent(ticket, 1, TimeSpan.FromSeconds(1), true), Is.False);
    }

    [Test]
    public void LatestMessageIsBoundedForDegradedBudgetWithoutEmptyInput()
    {
        var store = Store(); var view = Open(store);
        store.Accept(Player, 1, false, new string('a', 500), false, TimeSpan.Zero);
        var ticket = store.Begin(view, TimeSpan.Zero, 256)!;
        Assert.That(ticket.Conversation.Length, Is.EqualTo(256));
        Assert.That(AIDraftPrompt.Build(ticket, [], 1024).Length, Is.InRange(1, 1024));
    }

    [Test]
    public void FollowUpKeepsPlayerQuestionAndAcceptedAdministratorReply()
    {
        var store = Store();
        store.Accept(Player, 1, false, "Как вылечить морпеха?", false, TimeSpan.Zero);
        store.Accept(Player, 1, true, "Какой вид травмы?", false, TimeSpan.Zero);
        store.Accept(Player, 1, false, "Я не знаю, я новичок", false, TimeSpan.Zero);
        var view = store.Open(Owner, Player, Selection, 1, TimeSpan.Zero, true)!;
        var ticket = store.Begin(view, TimeSpan.Zero, 1200)!;
        Assert.That(ticket.Conversation, Does.Contain("Как вылечить морпеха?").And.Contain("Администратор: Какой вид травмы?").And.Contain("Я не знаю, я новичок"));
        Assert.That(ticket.Question, Is.EqualTo("Я не знаю, я новичок"));
        Assert.That(ticket.PlayerMessages, Is.EqualTo(new[] { "Как вылечить морпеха?", "Я не знаю, я новичок" }));
        Assert.That(ticket.HasHistory, Is.True);
        var request = AIDraftPrompt.Request(ticket, [], 2300, 128)!;
        Assert.That(request.Dialogue.Select(turn => turn.FromAdmin), Is.EqualTo(new[] { false, true, false }));
        Assert.That(request.Dialogue[^1].Text, Is.EqualTo("Я не знаю, я новичок"));
        Assert.That(request.Input, Does.Not.Contain("Администратор:").And.Not.Contain("Я не знаю"));
    }

    [Test]
    public void MessageAndCharacterEvictionPreserveOpeningTopicAndLatestTurn()
    {
        var store = Store();
        store.Accept(Player, 1, false, "Как вылечить морпеха?", false, TimeSpan.Zero);
        for (var i = 0; i < 20; i++)
            store.Accept(Player, 1, true, "Уточнение " + i + new string('a', 450), false, TimeSpan.Zero);
        store.Accept(Player, 1, false, "Я не знаю, я новичок", false, TimeSpan.Zero);
        var view = store.Open(Owner, Player, Selection, 1, TimeSpan.Zero, true)!;
        var ticket = store.Begin(view, TimeSpan.Zero, 1200)!;
        Assert.That(ticket.Conversation.Length, Is.LessThanOrEqualTo(1200));
        Assert.That(ticket.Conversation, Does.StartWith("Игрок: Как вылечить морпеха?\n"));
        Assert.That(ticket.Conversation, Does.EndWith("Игрок: Я не знаю, я новичок\n"));
        Assert.That(ticket.Conversation, Does.Not.Contain("Уточнение 0"));
        Assert.That(ticket.PlayerMessages, Has.Length.EqualTo(2));
    }

    [Test]
    public void KnowledgeSourcesAreActualExcerptsWithContentHash()
    {
        const string fragment = "Опубликованные правила запрещают метагейминг и разглашение информации текущего раунда.";
        var content = Encoding.UTF8.GetBytes("<Document>" + fragment + "</Document>");
        var knowledge = new AIRuleKnowledgeBase([new("AU14RulesGeneral", "Общие правила", "/ServerInfo/Guidebook/CMU14/Rules/test.xml", content)]);
        var sources = knowledge.Search("правила метагейминг");
        Assert.That(sources, Has.Length.EqualTo(1)); Assert.That(sources[0].Fragment, Is.EqualTo(fragment));
        Assert.That(sources[0].Version, Has.Length.EqualTo(64));
        Assert.That(sources[0].GuideId, Is.EqualTo("AU14RulesGeneral"));
        Assert.That(knowledge.Search("несуществующее понятие"), Is.Empty);
    }

    [Test]
    public void KnowledgeCannotReadOutsidePublishedScopeOrExpandExternalEntities()
    {
        var attack = Encoding.UTF8.GetBytes("<!DOCTYPE Document [<!ENTITY secret SYSTEM 'file:///etc/passwd'>]><Document>&secret;</Document>");
        var knowledge = new AIRuleKnowledgeBase([
            new("bad", "bad", "/ServerInfo/Guidebook/CMU14/Rules/test.xml", attack),
            new("private", "private", "/AdminNotes/private.xml", Encoding.UTF8.GetBytes("<Document>secret admin data forbidden source</Document>")),
            new("oversized", "oversized", "/ServerInfo/Guidebook/CMU14/Rules/test.xml", new byte[65537]),
        ]);
        Assert.That(knowledge.Documents, Is.Zero); Assert.That(knowledge.Search("secret admin data"), Is.Empty);
        Assert.That(AIRuleKnowledgeBase.IsAllowedPath("/ServerInfo/Guidebook/CMU14/Rules/../../private.xml"), Is.False);
    }

    [Test]
    public void MarkupLinksControlAndBidiCharactersCannotBecomeActiveUiContent()
    {
        var text = AdminAIDraftText.Sanitize("[cmdlink command='ban']bad[/cmdlink] https://evil.example/secret\u202e\0\r\nplain");
        Assert.That(text.Contains('[', StringComparison.Ordinal), Is.False);
        Assert.That(text.Contains("https://", StringComparison.Ordinal), Is.False);
        Assert.That(text.Contains("\u202e", StringComparison.Ordinal), Is.False);
        Assert.That(text.IndexOf('\0'), Is.EqualTo(-1));
        Assert.That(text, Does.Contain("plain"));
        Assert.That(AdminAIDraftText.Sanitize(new string('a', 4097)), Is.Empty);
    }

    [Test]
    public void CapacityEvictionNotifiesTheExactRemovedViewAndInvalidatesItsTicket()
    {
        var store = new AIDraftSessionStore(new(MaxConversations: 1)); var player = Guid.NewGuid(); var owner = Guid.NewGuid();
        store.Accept(player, 7, false, "Первое обращение", false, TimeSpan.Zero);
        var view = store.Open(owner, player, Guid.NewGuid(), 7, TimeSpan.Zero, true)!;
        var ticket = store.Begin(view, TimeSpan.Zero, 800)!;
        AIDraftSessionStore.View? removed = null; store.ViewRemoved += value => removed = value;
        store.Accept(Guid.NewGuid(), 7, false, "Другое обращение", false, TimeSpan.FromSeconds(1));
        Assert.That(removed, Is.SameAs(view)); Assert.That(removed!.SelectionId, Is.EqualTo(view.SelectionId));
        Assert.That(store.Peek(owner), Is.Null);
        Assert.That(store.Complete(ticket, 7, TimeSpan.FromSeconds(1), true, AIInferenceStatus.Success, "late draft"), Is.Null);
    }
}
