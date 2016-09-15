import { bindable, autoinject } from 'aurelia-framework';
import { EventAggregator, Subscription } from 'aurelia-event-aggregator';
import { Router, RouterConfiguration } from 'aurelia-router';

import { ChatService } from '../services/chat.service';
import { Conversation } from '../model/conversation';
import { Message } from '../model/message';
import { ConversationSelected } from '../events/conversationSelected';
import { MessageReceived } from '../events/messageReceived';

@autoinject
export class ConversationPreview {
    @bindable conversation: Conversation;
    isSelected: boolean;
    
    private conversationSelectedSubscription: Subscription;
    private messageReceivedSubscription: Subscription;

    constructor(private service: ChatService, private ea: EventAggregator, private router: Router) { }

    select() {
        this.service.showConversation(this.conversation, this.router);
    }

    attached() {
        this.conversationSelectedSubscription = this.ea.subscribe(ConversationSelected, e => {
            if (e.conversation.id === this.conversation.id) {
                this.isSelected = true;
            } else {
                this.isSelected = false;
            }
        });
        this.messageReceivedSubscription = this.ea.subscribe(MessageReceived, e => {
            let message = e.messaqe as Message;
            if (message.conversationId === this.conversation.id) {
                this.conversation.messages.unshift(e.message);
            }
        });
    }

    detached() {
        this.conversationSelectedSubscription.dispose();
        this.messageReceivedSubscription.dispose();
    }
}