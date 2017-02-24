﻿
import { StrategyRuleSetInfo } from "./rule-models";
import { ArticleBlockInfo } from "./article-models";

export class StrategySummary {
    active: boolean;
    url: string;
    summary: string;
    strategyId: number;
    title: string;

    selected?: boolean;

    constructor() {
        this.selected = false;
    }
}

export interface StrategyInfo {
    ruleSets: StrategyRuleSetInfo[];
    blocks: ArticleBlockInfo[];
    strategyId: number;
    title: string;
    deleted: boolean;
    summary: string;
    active: boolean;
    url: string;
}

export interface StrategyEntity {
    strategyId: number;
    bane: string;
    url: string;
    jsonArticleBlocks: string;
    description: string;
    deleted: boolean;
    active: boolean;
}