/// <reference path="../../BlazorFluentUI.BFUBaseComponent/wwwroot/baseComponent.ts" />

type IRectangle = BlazorFluentUiBaseComponent.IRectangle;

namespace BlazorFluentUiMarqueeSelection {

    interface DotNetReferenceType {
        invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
        invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    }

    interface Point {
        top: number;
        left: number;
    }

    export function getDistanceBetweenPoints(point1: Point, point2: Point): number {
        const left1 = point1.left || 0;
        const top1 = point1.top || 0;
        const left2 = point2.left || 0;
        const top2 = point2.top || 0;

        let distance = Math.sqrt(Math.pow(left1 - left2, 2) + Math.pow(top1 - top2, 2));

        return distance;
    }

    interface EventParams {
        element: HTMLElement | Window;
        event: string;
        handler: (ev: Event) => void;
        capture: boolean;
    }

    export function assign(target: any, ...args: any[]): any {
        return filteredAssign.apply(this, [null, target].concat(args));
    }

    export function filteredAssign(isAllowed: (propName: string) => boolean, target: any, ...args: any[]): any {
        target = target || {};

        for (let sourceObject of args) {
            if (sourceObject) {
                for (let propName in sourceObject) {
                    if (sourceObject.hasOwnProperty(propName) && (!isAllowed || isAllowed(propName))) {
                        target[propName] = sourceObject[propName];
                    }
                }
            }
        }

        return target;
    }

    export interface IEventRecord {
        target: any;
        eventName: string;
        parent: any;
        callback: (args?: any) => void;
        elementCallback?: (...args: any[]) => void;
        objectCallback?: (args?: any) => void;
        options?: boolean | AddEventListenerOptions;
    }

    export interface IEventRecordsByName {
        [eventName: string]: IEventRecordList;
    }

    export interface IEventRecordList {
        [id: string]: IEventRecord[] | number;
        count: number;
    }

    export interface IDeclaredEventsByName {
        [eventName: string]: boolean;
    }

    /** An instance of EventGroup allows anything with a handle to it to trigger events on it.
     *  If the target is an HTMLElement, the event will be attached to the element and can be
     *  triggered as usual (like clicking for onClick).
     *  The event can be triggered by calling EventGroup.raise() here. If the target is an
     *  HTMLElement, the event gets raised and is handled by the browser. Otherwise, it gets
     *  handled here in EventGroup, and the handler is called in the context of the parent
     *  (which is passed in in the constructor).
     *
     * @public
     * {@docCategory EventGroup}
     */
    export class EventGroup {
        private static _uniqueId: number = 0;
        // tslint:disable-next-line:no-any
        private _parent: any;
        private _eventRecords: IEventRecord[];
        private _id: number = EventGroup._uniqueId++;
        private _isDisposed: boolean;

        /** For IE8, bubbleEvent is ignored here and must be dealt with by the handler.
         *  Events raised here by default have bubbling set to false and cancelable set to true.
         *  This applies also to built-in events being raised manually here on HTMLElements,
         *  which may lead to unexpected behavior if it differs from the defaults.
         *
         */
        public static raise(
            // tslint:disable-next-line:no-any
            target: any,
            eventName: string,
            // tslint:disable-next-line:no-any
            eventArgs?: any,
            bubbleEvent?: boolean,
        ): boolean | undefined {
            let retVal;

            if (EventGroup._isElement(target)) {
                if (typeof document !== 'undefined' && document.createEvent) {
                    let ev = document.createEvent('HTMLEvents');

                    ev.initEvent(eventName, bubbleEvent || false, true);

                    assign(ev, eventArgs);

                    retVal = target.dispatchEvent(ev);
                    // tslint:disable-next-line:no-any
                } else if (typeof document !== 'undefined' && (document as any)['createEventObject']) {
                    // IE8
                    // tslint:disable-next-line:no-any
                    let evObj = (document as any)['createEventObject'](eventArgs);
                    // cannot set cancelBubble on evObj, fireEvent will overwrite it
                    target.fireEvent('on' + eventName, evObj);
                }
            } else {
                while (target && retVal !== false) {
                    let events = <IEventRecordsByName>target.__events__;
                    let eventRecords = events ? events[eventName] : null;

                    if (eventRecords) {
                        for (let id in eventRecords) {
                            if (eventRecords.hasOwnProperty(id)) {
                                let eventRecordList = <IEventRecord[]>eventRecords[id];

                                for (let listIndex = 0; retVal !== false && listIndex < eventRecordList.length; listIndex++) {
                                    let record = eventRecordList[listIndex];

                                    if (record.objectCallback) {
                                        retVal = record.objectCallback.call(record.parent, eventArgs);
                                    }
                                }
                            }
                        }
                    }

                    // If the target has a parent, bubble the event up.
                    target = bubbleEvent ? target.parent : null;
                }
            }

            return retVal;
        }

        // tslint:disable-next-line:no-any
        public static isObserved(target: any, eventName: string): boolean {
            let events = target && <IEventRecordsByName>target.__events__;

            return !!events && !!events[eventName];
        }

        /** Check to see if the target has declared support of the given event. */
        // tslint:disable-next-line:no-any
        public static isDeclared(target: any, eventName: string): boolean {
            let declaredEvents = target && <IDeclaredEventsByName>target.__declaredEvents;

            return !!declaredEvents && !!declaredEvents[eventName];
        }

        // tslint:disable-next-line:no-any
        public static stopPropagation(event: any): void {
            if (event.stopPropagation) {
                event.stopPropagation();
            } else {
                // IE8
                event.cancelBubble = true;
            }
        }

        private static _isElement(target: HTMLElement): boolean {
            return (
                !!target && (!!target.addEventListener || (typeof HTMLElement !== 'undefined' && target instanceof HTMLElement))
            );
        }

        /** parent: the context in which events attached to non-HTMLElements are called */
        // tslint:disable-next-line:no-any
        public constructor(parent: any) {
            this._parent = parent;
            this._eventRecords = [];
        }

        public dispose(): void {
            if (!this._isDisposed) {
                this._isDisposed = true;

                this.off();
                this._parent = null;
            }
        }

        /** On the target, attach a set of events, where the events object is a name to function mapping. */
        // tslint:disable-next-line:no-any
        public onAll(target: any, events: { [key: string]: (args?: any) => void }, useCapture?: boolean): void {
            for (let eventName in events) {
                if (events.hasOwnProperty(eventName)) {
                    this.on(target, eventName, events[eventName], useCapture);
                }
            }
        }

        /**
         * On the target, attach an event whose handler will be called in the context of the parent
         * of this instance of EventGroup.
         */
        public on(
            target: any, // tslint:disable-line:no-any
            eventName: string,
            callback: (args?: any) => void, // tslint:disable-line:no-any
            options?: boolean | AddEventListenerOptions,
        ): void {
            if (eventName.indexOf(',') > -1) {
                let events = eventName.split(/[ ,]+/);

                for (let i = 0; i < events.length; i++) {
                    this.on(target, events[i], callback, options);
                }
            } else {
                let parent = this._parent;
                let eventRecord: IEventRecord = {
                    target: target,
                    eventName: eventName,
                    parent: parent,
                    callback: callback,
                    options,
                };

                // Initialize and wire up the record on the target, so that it can call the callback if the event fires.
                let events = <IEventRecordsByName>(target.__events__ = target.__events__ || {});
                events[eventName] =
                    events[eventName] ||
                    <IEventRecordList>{
                        count: 0,
                    };
                events[eventName][this._id] = events[eventName][this._id] || [];
                (<IEventRecord[]>events[eventName][this._id]).push(eventRecord);
                events[eventName].count++;

                if (EventGroup._isElement(target)) {
                    // tslint:disable-next-line:no-any
                    let processElementEvent = (...args: any[]) => {
                        if (this._isDisposed) {
                            return;
                        }

                        let result;
                        try {
                            result = callback.apply(parent, args);
                            if (result === false && args[0]) {
                                let e = args[0];

                                if (e.preventDefault) {
                                    e.preventDefault();
                                }

                                if (e.stopPropagation) {
                                    e.stopPropagation();
                                }

                                e.cancelBubble = true;
                            }
                        } catch (e) {
                            /* ErrorHelper.log(e); */
                        }

                        return result;
                    };

                    eventRecord.elementCallback = processElementEvent;

                    if (target.addEventListener) {
                        /* tslint:disable:ban-native-functions */
                        (<EventTarget>target).addEventListener(eventName, processElementEvent, options);
                        /* tslint:enable:ban-native-functions */
                    } else if (target.attachEvent) {
                        // IE8
                        target.attachEvent('on' + eventName, processElementEvent);
                    }
                } else {
                    // tslint:disable-next-line:no-any
                    let processObjectEvent = (...args: any[]) => {
                        if (this._isDisposed) {
                            return;
                        }

                        return callback.apply(parent, args);
                    };

                    eventRecord.objectCallback = processObjectEvent;
                }

                // Remember the record locally, so that it can be removed.
                this._eventRecords.push(eventRecord);
            }
        }

        public off(
            target?: any, // tslint:disable-line:no-any
            eventName?: string,
            callback?: (args?: any) => void, // tslint:disable-line:no-any
            options?: boolean | AddEventListenerOptions,
        ): void {
            for (let i = 0; i < this._eventRecords.length; i++) {
                let eventRecord = this._eventRecords[i];
                if (
                    (!target || target === eventRecord.target) &&
                    (!eventName || eventName === eventRecord.eventName) &&
                    (!callback || callback === eventRecord.callback) &&
                    (typeof options !== 'boolean' || options === eventRecord.options)
                ) {
                    let events = <IEventRecordsByName>eventRecord.target.__events__;
                    let targetArrayLookup = events[eventRecord.eventName];
                    let targetArray = targetArrayLookup ? <IEventRecord[]>targetArrayLookup[this._id] : null;

                    // We may have already target's entries, so check for null.
                    if (targetArray) {
                        if (targetArray.length === 1 || !callback) {
                            targetArrayLookup.count -= targetArray.length;
                            delete events[eventRecord.eventName][this._id];
                        } else {
                            targetArrayLookup.count--;
                            targetArray.splice(targetArray.indexOf(eventRecord), 1);
                        }

                        if (!targetArrayLookup.count) {
                            delete events[eventRecord.eventName];
                        }
                    }

                    if (eventRecord.elementCallback) {
                        if (eventRecord.target.removeEventListener) {
                            eventRecord.target.removeEventListener(
                                eventRecord.eventName,
                                eventRecord.elementCallback,
                                eventRecord.options,
                            );
                        } else if (eventRecord.target.detachEvent) {
                            // IE8
                            eventRecord.target.detachEvent('on' + eventRecord.eventName, eventRecord.elementCallback);
                        }
                    }

                    this._eventRecords.splice(i--, 1);
                }
            }
        }

        /** Trigger the given event in the context of this instance of EventGroup. */
        // tslint:disable-next-line:no-any
        public raise(eventName: string, eventArgs?: any, bubbleEvent?: boolean): boolean | undefined {
            return EventGroup.raise(this._parent, eventName, eventArgs, bubbleEvent);
        }

        /** Declare an event as being supported by this instance of EventGroup. */
        public declare(event: string | string[]): void {
            let declaredEvents = (this._parent.__declaredEvents = this._parent.__declaredEvents || {});

            if (typeof event === 'string') {
                declaredEvents[event] = true;
            } else {
                for (let i = 0; i < event.length; i++) {
                    declaredEvents[event[i]] = true;
                }
            }
        }
    }

    declare function setTimeout(cb: Function, delay: number): number;

    const SCROLL_ITERATION_DELAY = 16;
    const SCROLL_GUTTER = 100;
    const MAX_SCROLL_VELOCITY = 15;

    export function getRect(element: HTMLElement | Window | null): IRectangle | undefined {
        let rect: IRectangle | undefined;
        if (element) {
            if (element === window) {
                rect = {
                    left: 0,
                    top: 0,
                    width: window.innerWidth,
                    height: window.innerHeight,
                    right: window.innerWidth,
                    bottom: window.innerHeight,
                };
            } else if ((element as HTMLElement).getBoundingClientRect) {
                rect = (element as HTMLElement).getBoundingClientRect();
            }
        }
        return rect;
    }

    export class AutoScroll {
        private _events: EventGroup;
        private _scrollableParent: HTMLElement | null;
        private _scrollRect: IRectangle | undefined;
        private _scrollVelocity: number;
        private _isVerticalScroll: boolean;
        private _timeoutId: number;

        constructor(element: HTMLElement) {
            this._events = new EventGroup(this);
            this._scrollableParent = BlazorFluentUiBaseComponent.findScrollableParent(element) as HTMLElement;

            this._incrementScroll = this._incrementScroll.bind(this);
            this._scrollRect = getRect(this._scrollableParent);

            // tslint:disable-next-line:no-any
            if (this._scrollableParent === (window as any)) {
                this._scrollableParent = document.body;
            }

            if (this._scrollableParent) {
                this._events.on(window, 'mousemove', this._onMouseMove, true);
                this._events.on(window, 'touchmove', this._onTouchMove, true);
            }
        }

        public dispose(): void {
            this._events.dispose();
            this._stopScroll();
        }

        private _onMouseMove(ev: MouseEvent): void {
            this._computeScrollVelocity(ev);
        }

        private _onTouchMove(ev: TouchEvent): void {
            if (ev.touches.length > 0) {
                this._computeScrollVelocity(ev);
            }
        }

        private _computeScrollVelocity(ev: MouseEvent | TouchEvent): void {
            if (!this._scrollRect) {
                return;
            }

            let clientX: number;
            let clientY: number;
            if ('clientX' in ev) {
                clientX = ev.clientX;
                clientY = ev.clientY;
            } else {
                clientX = ev.touches[0].clientX;
                clientY = ev.touches[0].clientY;
            }

            let scrollRectTop = this._scrollRect.top;
            let scrollRectLeft = this._scrollRect.left;
            let scrollClientBottom = scrollRectTop + this._scrollRect.height - SCROLL_GUTTER;
            let scrollClientRight = scrollRectLeft + this._scrollRect.width - SCROLL_GUTTER;

            // variables to use for alternating scroll direction
            let scrollRect;
            let clientDirection;
            let scrollClient;

            // if either of these conditions are met we are scrolling vertically else horizontally
            if (clientY < scrollRectTop + SCROLL_GUTTER || clientY > scrollClientBottom) {
                clientDirection = clientY;
                scrollRect = scrollRectTop;
                scrollClient = scrollClientBottom;
                this._isVerticalScroll = true;
            } else {
                clientDirection = clientX;
                scrollRect = scrollRectLeft;
                scrollClient = scrollClientRight;
                this._isVerticalScroll = false;
            }

            // calculate scroll velocity and direction
            if (clientDirection! < scrollRect + SCROLL_GUTTER) {
                this._scrollVelocity = Math.max(
                    -MAX_SCROLL_VELOCITY,
                    -MAX_SCROLL_VELOCITY * ((SCROLL_GUTTER - (clientDirection - scrollRect)) / SCROLL_GUTTER),
                );
            } else if (clientDirection > scrollClient) {
                this._scrollVelocity = Math.min(
                    MAX_SCROLL_VELOCITY,
                    MAX_SCROLL_VELOCITY * ((clientDirection - scrollClient) / SCROLL_GUTTER),
                );
            } else {
                this._scrollVelocity = 0;
            }

            if (this._scrollVelocity) {
                this._startScroll();
            } else {
                this._stopScroll();
            }
        }

        private _startScroll(): void {
            if (!this._timeoutId) {
                this._incrementScroll();
            }
        }

        private _incrementScroll(): void {
            if (this._scrollableParent) {
                if (this._isVerticalScroll) {
                    this._scrollableParent.scrollTop += Math.round(this._scrollVelocity);
                } else {
                    this._scrollableParent.scrollLeft += Math.round(this._scrollVelocity);
                }
            }

            this._timeoutId = setTimeout(this._incrementScroll, SCROLL_ITERATION_DELAY);
        }

        private _stopScroll(): void {
            if (this._timeoutId) {
                clearTimeout(this._timeoutId);
                delete this._timeoutId;
            }
        }
    }

    class Handler {

        static objectListeners: Map<DotNetReferenceType, Map<string, EventParams>> = new Map<DotNetReferenceType, Map<string, EventParams>>();

        static addListener(ref: DotNetReferenceType, element: HTMLElement | Window, event: string, handler: (ev: Event) => void, capture: boolean): void {
            let listeners: Map<string, EventParams>;
            if (this.objectListeners.has(ref)) {
                listeners = this.objectListeners.get(ref);
            } else {
                listeners = new Map<string, EventParams>();
                this.objectListeners.set(ref, listeners);
            }
            element.addEventListener(event, handler, capture);
            listeners.set(event, { capture: capture, event: event, handler: handler, element: element });
        }
        static removeListener(ref: DotNetReferenceType, event: string): void {
            if (this.objectListeners.has(ref)) {
                let listeners = this.objectListeners.get(ref);
                if (listeners.has(event)) {
                    var handler = listeners.get(event);
                    handler.element.removeEventListener(handler.event, handler.handler, handler.capture);
                }
                listeners.delete[event];
            }
        }
    }

    const marqueeSelections = new Map<DotNetReferenceType, MarqueeSelection>();

    export function registerMarqueeSelection(dotNet: DotNetReferenceType, root: HTMLElement, props:IMarqueeSelectionProps) {
        let marqueeSelection = new MarqueeSelection(dotNet, root, props);
        marqueeSelections.set(dotNet, marqueeSelection);
    }

    export function updateProps(dotNet: DotNetReferenceType, props: IMarqueeSelectionProps) {
        //assume itemsource may have changed... 

    }

    export function unregisterMarqueeSelection(dotNet: DotNetReferenceType) {
        let marqueeSelection = marqueeSelections.get(dotNet);
        marqueeSelection.dispose();
        marqueeSelections.delete(dotNet);
    }



    interface IMarqueeSelectionProps {
        isDraggingConstrainedToRoot: boolean;
        isEnabled: boolean;
    }

    const MIN_DRAG_DISTANCE = 5;

    class MarqueeSelection {

        dotNet: DotNetReferenceType;
        root: HTMLElement;
        scrollableParent: HTMLElement;
        scrollableSurface: HTMLElement;
        isTouch: boolean;

        events: EventGroup;
        autoScroll: AutoScroll;
        _async: BlazorFluentUiBaseComponent.Async;

        props: IMarqueeSelectionProps;
        dragRect: IRectangle;

        animationFrameRequest: number;

        _lastMouseEvent: MouseEvent | undefined;
        _selectedIndicies: { [key: string]: boolean } | undefined;
        _preservedIndicies: number[] | undefined;
        _scrollTop: number;
        _scrollLeft: number;
        _rootRect: IRectangle;
        _dragOrigin: Point | undefined;
        _itemRectCache: { [key: string]: IRectangle } | undefined;
        _allSelectedIndices: { [key: string]: boolean } | undefined;

        _mirroredDragRect: IRectangle;

        constructor(dotNet: DotNetReferenceType, root: HTMLElement, props: IMarqueeSelectionProps) {
            this.dotNet = dotNet;
            this.root = root;
            this.props = props;

            this.events = new EventGroup(this);
            this._async = new BlazorFluentUiBaseComponent.Async(this);

            this.scrollableParent = BlazorFluentUiBaseComponent.findScrollableParent(root);
            this.scrollableSurface = this.scrollableParent === (window as any) ? document.body : this.scrollableParent;

            const hitTarget = props.isDraggingConstrainedToRoot ? this.root : this.scrollableSurface;

            this.events.on(hitTarget, 'mousedown', this.onMouseDown);
            //this.events.on(hitTarget, 'touchstart', this.onTouchStart, true);
            //this.events.on(hitTarget, 'pointerdown', this.onPointerDown, true);
        }

        public updateProps(props: IMarqueeSelectionProps): void {
            this.props = props;
            this._itemRectCache = {};
        }

        public dispose(): void {
            if (this.autoScroll) {
                this.autoScroll.dispose();
            }
            delete this.scrollableParent;
            delete this.scrollableSurface;

            this.events.dispose();
            this._async.dispose();
        }

        private _isMouseEventOnScrollbar(ev: MouseEvent): boolean {
            const targetElement = ev.target as HTMLElement;
            const targetScrollbarWidth = targetElement.offsetWidth - targetElement.clientWidth;

            if (targetScrollbarWidth) {
                const targetRect = targetElement.getBoundingClientRect();

                // Check vertical scroll
                //if (getRTL(this.props.theme)) {
                //    if (ev.clientX < targetRect.left + targetScrollbarWidth) {
                //        return true;
                //    }
                //} else {
                    if (ev.clientX > targetRect.left + targetElement.clientWidth) {
                        return true;
                    }
                //}

                // Check horizontal scroll
                if (ev.clientY > targetRect.top + targetElement.clientHeight) {
                    return true;
                }
            }

            return false;
        }

        private onMouseDown = async (ev: MouseEvent): Promise<void> => {

            // Ensure the mousedown is within the boundaries of the target. If not, it may have been a click on a scrollbar.
            if (this._isMouseEventOnScrollbar(ev)) {
                return;
            }
            if (this._isInSelectionToggle(ev)) {
                return;
            }

            if (
                !this.isTouch &&
                this.props.isEnabled &&
                !this._isDragStartInSelection(ev)) {

                let shouldStart = await this.dotNet.invokeMethodAsync<boolean>("OnShouldStartSelectionInternal");
                if (shouldStart) {

                    if (this.scrollableSurface && ev.button === 0 && this.root) {
                        this._selectedIndicies = {};
                        this._preservedIndicies = undefined;
                        this.events.on(window, 'mousemove', this._onAsyncMouseMove, true);
                        this.events.on(this.scrollableParent, 'scroll', this._onAsyncMouseMove);
                        this.events.on(window, 'click', this.onMouseUp, true);

                        this.autoScroll = new AutoScroll(this.root);
                        this._scrollTop = this.scrollableSurface.scrollTop;
                        this._scrollLeft = this.scrollableSurface.scrollLeft;
                        this._rootRect = this.root.getBoundingClientRect();

                        this._onMouseMove(ev);
                    }
                }
            }

        }

        private _getRootRect(): IRectangle {
            return {
                left: this._rootRect.left + (this._scrollLeft - this.scrollableSurface.scrollLeft),
                top: this._rootRect.top + (this._scrollTop - this.scrollableSurface.scrollTop),
                width: this._rootRect.width,
                height: this._rootRect.height,
            };
        }

        private _onAsyncMouseMove(ev: MouseEvent): void {
            this.animationFrameRequest = window.requestAnimationFrame(() => {
                this._onMouseMove(ev);
            });
            
            ev.stopPropagation();
            ev.preventDefault();
        }

        private _onMouseMove(ev: MouseEvent): boolean | undefined {
            if (!this.autoScroll) {
                return;
            }

            if (ev.clientX !== undefined) {
                this._lastMouseEvent = ev;
            }

            const rootRect = this._getRootRect();
            const currentPoint = { left: ev.clientX - rootRect.left, top: ev.clientY - rootRect.top };

            if (!this._dragOrigin) {
                this._dragOrigin = currentPoint;
            }

            if (ev.buttons !== undefined && ev.buttons === 0) {
                this.onMouseUp(ev);
            } else {
                if (this._mirroredDragRect || getDistanceBetweenPoints(this._dragOrigin, currentPoint) > MIN_DRAG_DISTANCE) {
                    if (!this._mirroredDragRect) {
                        //const { selection } = this.props;

                        if (!ev.shiftKey) {
                            this.dotNet.invokeMethodAsync("UnselectAll");
                            //selection.setAllSelected(false);
                        }

                        //this._preservedIndicies =  selection && selection.getSelectedIndices && selection.getSelectedIndices();
                    }

                    // We need to constrain the current point to the rootRect boundaries.
                    const constrainedPoint = this.props.isDraggingConstrainedToRoot
                        ? {
                            left: Math.max(0, Math.min(rootRect.width, this._lastMouseEvent!.clientX - rootRect.left)),
                            top: Math.max(0, Math.min(rootRect.height, this._lastMouseEvent!.clientY - rootRect.top)),
                        }
                        : {
                            left: this._lastMouseEvent!.clientX - rootRect.left,
                            top: this._lastMouseEvent!.clientY - rootRect.top,
                        };

                    this.dragRect = {
                        left: Math.min(this._dragOrigin.left || 0, constrainedPoint.left),
                        top: Math.min(this._dragOrigin.top || 0, constrainedPoint.top),
                        width: Math.abs(constrainedPoint.left - (this._dragOrigin.left || 0)),
                        height: Math.abs(constrainedPoint.top - (this._dragOrigin.top || 0)),
                    };

                    this._evaluateSelection(this.dragRect, rootRect);

                    this.dotNet.invokeMethodAsync("SetDragRect", this.dragRect);
                    //this.setState({ dragRect });
                }
            }

            return false;
        }

        private _evaluateSelection(dragRect: IRectangle, rootRect: IRectangle): void {
            // Break early if we don't need to evaluate.
            if (!dragRect || !this.root) {
                return;
            }

            const allElements = this.root.querySelectorAll('[data-item-index]');

            if (!this._itemRectCache) {
                this._itemRectCache = {};
            }

            for (let i = 0; i < allElements.length; i++) {
                const element = allElements[i];
                const index = element.getAttribute('data-item-index') as string;

                // Pull the memoized rectangle for the item, or the get the rect and memoize.
                let itemRect = this._itemRectCache[index];

                if (!itemRect) {
                    itemRect = element.getBoundingClientRect();

                    // Normalize the item rect to the dragRect coordinates.
                    itemRect = {
                        left: itemRect.left - rootRect.left,
                        top: itemRect.top - rootRect.top,
                        width: itemRect.width,
                        height: itemRect.height,
                        right: itemRect.left - rootRect.left + itemRect.width,
                        bottom: itemRect.top - rootRect.top + itemRect.height,
                    };

                    if (itemRect.width > 0 && itemRect.height > 0) {
                        this._itemRectCache[index] = itemRect;
                    }
                }

                if (
                    itemRect.top < dragRect.top + dragRect.height &&
                    itemRect.bottom! > dragRect.top &&
                    itemRect.left < dragRect.left + dragRect.width &&
                    itemRect.right! > dragRect.left
                ) {
                    this._selectedIndicies![index] = true;
                } else {
                    delete this._selectedIndicies![index];
                }
            }

            // set previousSelectedIndices to be all of the selected indices from last time
            const previousSelectedIndices = this._allSelectedIndices || {};
            this._allSelectedIndices = {};

            // set all indices that are supposed to be selected in _allSelectedIndices
            for (const index in this._selectedIndicies!) {
                if (this._selectedIndicies!.hasOwnProperty(index)) {
                    this._allSelectedIndices![index] = true;
                }
            }

            if (this._preservedIndicies) {
                for (const index of this._preservedIndicies!) {
                    this._allSelectedIndices![index] = true;
                }
            }

            // check if needs to update selection, only when current _allSelectedIndices
            // is different than previousSelectedIndices
            let needToUpdate = false;
            for (const index in this._allSelectedIndices!) {
                if (this._allSelectedIndices![index] !== previousSelectedIndices![index]) {
                    needToUpdate = true;
                    break;
                }
            }

            if (!needToUpdate) {
                for (const index in previousSelectedIndices!) {
                    if (this._allSelectedIndices![index] !== previousSelectedIndices![index]) {
                        needToUpdate = true;
                        break;
                    }
                }
            }

            // only update selection when needed
            if (needToUpdate) {
                // Stop change events, clear selection to re-populate.
                //selection.setChangeEvents(false);
                //selection.setAllSelected(false);

                //for (const index of Object.keys(this._allSelectedIndices!)) {
                //    selection.setIndexSelected(Number(index), true, false);
                //}

                //selection.setChangeEvents(true);
            }
        }

        private onMouseUp(ev: MouseEvent): void {
            this.events.off(window);
            this.events.off(this.scrollableParent, 'scroll');

            if (this.autoScroll) {
                this.autoScroll.dispose();
            }

            this.autoScroll = this._dragOrigin = this._lastMouseEvent = undefined;
            this._selectedIndicies = this._itemRectCache = undefined;

            if (this.dragRect) {
                //this.setState({
                //    dragRect: undefined,
                //});
                this.dotNet.invokeMethodAsync("SetDragRect", this.dragRect);

                ev.preventDefault();
                ev.stopPropagation();
            }
        }

        private _isInSelectionToggle(ev: MouseEvent): boolean {
            let element: HTMLElement | null = ev.target as HTMLElement;

            while (element && element !== this.root) {
                if (element.getAttribute('data-selection-toggle') === 'true') {
                    return true;
                }

                element = element.parentElement;
            }

            return false;
        }

        /**
   * We do not want to start the marquee if we're trying to marquee
   * from within an existing marquee selection.
   */
        private _isDragStartInSelection(ev: MouseEvent): boolean {

            const selectedElements = this.root.querySelectorAll('[data-is-selected]');
            for (let i = 0; i < selectedElements.length; i++) {
                const element = selectedElements[i];
                const itemRect = element.getBoundingClientRect();
                if (this._isPointInRectangle(itemRect, { left: ev.clientX, top: ev.clientY })) {
                    return true;
                }
            }

            return false;
        }

        private _isPointInRectangle(rectangle: IRectangle, point: Point): boolean {
            return (
                !!point.top &&
                rectangle.top < point.top &&
                rectangle.bottom! > point.top &&
                !!point.left &&
                rectangle.left < point.left &&
                rectangle.right! > point.left
            );
        }


    }


}

(<any>window)['BlazorFluentUiMarqueeSelection'] = BlazorFluentUiMarqueeSelection || {};

