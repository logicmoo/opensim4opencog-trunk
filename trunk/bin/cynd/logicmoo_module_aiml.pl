% ===================================================================
% File 'logicmoo_module_aiml.pl'
% Purpose: An Implementation in SWI-Prolog of AIML
% Maintainer: Douglas Miles
% Contact: $Author: dmiles $@users.sourceforge.net ;
% Version: 'logicmoo_module_aiml.pl' 1.0.0
% Revision:  $Revision: 1.7 $
% Revised At:   $Date: 2002/07/11 21:57:28 $
% ===================================================================

%:-module()
%:-include('logicmoo_utils_header.pl'). %<?
%:- style_check(-singleton).
%%:- style_check(-discontiguous).
:- style_check(-atom).
:- style_check(-string).

:-multifile(what/3).
:-multifile(response/2).
:-dynamic(lineInfoElement/4).

run_chat_tests:-
   test_call(alicebot('Hi')),
   test_call(alicebot('What is your name')),
   test_call(alicebot('My name is Fred.')),
   test_call(alicebot('what is my name?')).

test_call(G):-writeln(G),ignore(once(catch(G,E,writeln(E)))).

main_loop1(Atom):- current_input(In),!,
            read_line_to_codes(In,Codes),!,
            atom_codes(Atom,Codes),!,
            alicebot(Atom),!.

main_loop:-repeat,main_loop1(_),fail.

% ===================================================================
%  aimlCate database decl
% ===================================================================

:-dynamic(aimlCateSigCached/1).
aimlCateSig(X):-aimlCateSigCached(X),!.
aimlCateSig(Pred):-aimlCateOrder(List),length(List,L),functor(Pred,aimlCate,L),asserta(aimlCateSigCached(Pred)),!.

aimlCateOrder([graph,precall,topic,that,request,pattern,flags,call,guard,userdict,template,srcinfo,srcfile]).

% [graph,precall,topic,that,pattern,flags,call,guard,template,userdict]
cateMemberTags(Result):- aimlCateOrder(List), findall(E,(member(E0,List),once((E0=[E|_];E0=E))), Result).


makeAimlCateSig(Ctx,ListOfValues,Pred):-aimlCateSig(Pred),!,makeAimlCate(Ctx,ListOfValues,Pred,current_value),!.

:- aimlCateOrder(List),length(List,L),dynamic(aimlCate/L),multifile(aimlCate/L). 

:-dynamic(default_channel/1).
:-dynamic(default_user/1).

%default_channel( "#logicmoo").
%default_user(    "default_user").         

% say(Say):-writeq(Say),nl.

% ===============================================================================================
% ALICE IN PROLOG
% ===============================================================================================

say(X):-currentContext(say(X),Ctx),say(Ctx,X),!.
say(Ctx,X):- aiml_eval(Ctx,X,Y),!,answerOutput(Y,O),debugFmt(say(O,Y)),!.

alicebot:-repeat,
	read_line_with_nl(user,CodesIn,[]),        
        makeAimlContext(alicebot,Ctx),
        once((trim(CodesIn,Codes),atom_codes(Atom,Codes),alicebotCTX(Ctx,Atom))),fail.

alicebot(Input):- currentContext(alicebot(Input),Ctx),!,alicebotCTX(Ctx,Input).

alicebotCTX(_Ctx,Input):- atom(Input),catch(((atom_to_term(Input,Term,Vars),callInteractive0(Term,Vars))),_,fail),!.
alicebotCTX(Ctx,Input):- alicebotCTX(Ctx,Input,Resp),!,say(Ctx,Resp),!.
%%alicebotCTX(Ctx,_):- trace, say(Ctx,'-no response-').


alicebotCTX(_Ctx,[],_):-debugFmt('no input'),!,fail.
alicebotCTX(Ctx,Input,Resp):- atom(Input),!,
      atomSplit(Input,TokensO),!,Tokens=TokensO,
      alicebotCTX(Ctx,Tokens,Resp),!.
alicebotCTX(Ctx,[TOK|Tokens],Output):- atom(TOK),atom_concat('@',_,TOK),!,systemCall(Ctx,'bot',[TOK|Tokens],Output),debugFmt(Output).
alicebotCTX(Ctx,Tokens,Resp):-
   %%convertToMatchable(Tokens,UCase),!,
   =(Tokens,UCase),!,
   removePMark(UCase,Atoms),!,
   alicebot2(Ctx,Atoms,Resp),!.
alicebotCTX(_Ctx,In,Res):- !,ignore(Res='-no response-'(In)).




alicebot2(Atoms,Resp):- currentContext(alicebot2(Atoms),Ctx),!,alicebot2(Ctx,Atoms,Resp).

alicebot2(Ctx,Atoms,Resp):-	
   retractall(posibleResponse(_,_)),
   flag(a_answers,_,0),!,
   ignore((

   call_with_depth_limit(computeInputOutput(Ctx,1,Atoms,Output,N),8000,_DL),
	 ignore((nonvar(N),nonvar(Output),savePosibleResponse(N,Output))),flag(a_answers,X,X+1),X>3)),!,
   findall(NR-OR,posibleResponse(NR,OR),L),!,
   (format('~n-> ~w~n',[L])),
   keysort(L,S),
   dumpList(S),
   reverse(S,[Resp|_RR]),
   degrade(Resp),!,
   rememberSaidIt(Resp),!.

computeInputOutput(Ctx,VoteIn,Input,Output,VotesOut):- computeAnswer(Ctx,VoteIn,element(srai,[],Input),Output,VotesOut).

% ===============================================================================================
% Save Possible Responses (Degrade them as well)
% ===============================================================================================
:-dynamic(posibleResponse/2).

savePosibleResponse(_N,Output):-posibleResponse(_,Output),!.
savePosibleResponse(N,Output):-
   findall(1,degraded(Output),L),!,
   length(L,K),
   SN is N - (K * 0.6)  , !,
   asserta(posibleResponse(SN,Output)).


% ===============================================================================================
% Expand Answers
% ===============================================================================================
flatten_if_list(List,Flat):-is_list(List),!,flatten(List,Flat),!.

computeTemplate(Ctx,Votes,Input,Output,VotesO):-flatten_if_list(Input,Flat),!,computeTemplate0(Ctx,Votes,Flat,Output,VotesO).
computeTemplate(Ctx,Votes,Input,Output,VotesO):-computeTemplate0(Ctx,Votes,Input,Output,VotesO).

computeTemplate0(Ctx,Votes,Input,Output,VotesO):-computeTemplate1(Ctx,Votes,Input,Output,VotesO),!.
computeTemplate0(Ctx,Votes,Input,Output,VotesO):-trace,computeTemplate1(Ctx,Votes,Input,Output,VotesO),!.

computeTemplate1(_Ctx,Votes,[],[],Votes):-!.
computeTemplate1(Ctx,Votes,[A|B],OO,Votes):-expandVar(Ctx,A,AA),!,computeTemplate1(Ctx,Votes,B,BB,Votes),once(flatten([AA,BB],OO)).
computeTemplate1(Ctx,Votes,[A|B],[A|BB],Votes):-atomic(A),!,computeTemplate1(Ctx,Votes,B,BB,Votes).
computeTemplate1(Ctx,VotesM,Output,OO,VotesM):-expandVar(Ctx,Output,OO),!.
computeTemplate1(_Ctx,VotesM,Output,Output,VotesM):-!.


expandVar(_Ctx,Var,Var):-var(Var),!,trace.
expandVar(_Ctx,[Var|_],Var):- !,trace.
expandVar(_Ctx,nick,A):-!,default_user(B),!,from_atom_codes(A,B),!.
expandVar(_Ctx,person,A):-!,default_user(B),!,from_atom_codes(A,B),!.
expandVar(_Ctx,botnick,'jllykifsh'):-!.
expandVar(_Ctx,mynick,'jllykifsh'):-!.
expandVar(_Ctx,name=name,'jllykifsh'):-!.
expandVar(_Ctx,mychan,A):-!,default_channel(B),!,from_atom_codes(A,B),!.
expandVar(_Ctx,Resp,Resp):-atomic(Resp),!.
expandVar(Ctx,In,Out):-computeAnswer(Ctx,1,In,Out,_VotesO).


from_atom_codes(Atom,Atom):-atom(Atom),!.
from_atom_codes(Atom,Codes):-convert_to_string(Codes,Atom),!.
from_atom_codes(Atom,Codes):-atom_codes(Atom,Codes).


:-dynamic(recursiveTag/1).


notRecursiveTag(system).
notRecursiveTag(condition).
notRecursiveTag(li).

recursiveTag(random).
recursiveTag(srai).
recursiveTag(NoRec):-notRecursiveTag(NoRec),!,fail.

recursiveTag(_).
isAimlTag(result):-!,fail.
isAimlTag(proof):-!,fail.
isAimlTag(get).
isAimlTag(_).

computeInner(_Ctx, _Votes, In, Out) :- atom(In),!,Out=In.
computeInner(Ctx,Votes, In, Out) :- not(Out=(_-_)),!,computeAnswer(Ctx,Votes, In, Out, _VoteMid),!.
computeInner(Ctx,Votes, In, VoteMid-Out) :-trace, computeAnswer(Ctx,Votes, In, Out, VoteMid),!.

computeInnerEach(_Ctx, _Votes, In, Out) :- atom(In), !, Out=In.
computeInnerEach(Ctx, Votes, In, Out) :- debugOnFailureAiml(computeAnswer(Ctx,Votes, In, Out, _VoteMid)),!.
computeInnerEach(_Ctx, _Votes, In, Out) :- !, Out=In.


% ===============================================================================================
% Compute Answer Element Probilities
% ===============================================================================================
computeElementMust(Ctx,Votes,Tag,Attribs,InnerXml,Resp,VotesO):-computeElement(Ctx,Votes,Tag,Attribs,InnerXml,Resp,VotesO),!.
computeElementMust(Ctx,Votes,Tag,Attribs,InnerXml,Resp,VotesO):-trace,computeElement(Ctx,Votes,Tag,Attribs,InnerXml,Resp,VotesO),!.

computeAnswerMaybe(Ctx,Votes,element(Tag,Attribs,InnerXml),Output,VotesO):-!,computeElement(Ctx,Votes,Tag,Attribs,InnerXml,Output,VotesO),!.
computeAnswerMaybe(Ctx,Votes,InnerXml,Resp,VotesO):-computeAnswer(Ctx,Votes,InnerXml,Resp,VotesO),!.

unused_computeElement(Ctx,Votes, Tag, ATTRIBS, [DO|IT], OUT, VotesO) :- recursiveTag(Tag),!,
      withAttributes(_Ctx,ATTRIBS,
        ((findall(Out,((member(In,[DO|IT]),computeTemplate(Ctx,Votes, In, Out, _VoteMid))),INNERDONE),
         NOUT=..[Tag,ATTRIBS,INNERDONE],!,
         computeAnswer(Ctx,Votes,NOUT,OUT,VotesO)))).

% element inner reductions
still_computeElement(Ctx,Votes, Tag, ATTRIBS, [DO|IT], OUT, VotesO) :- recursiveTag(Tag),not(DO=(_-_)),!,
     appendAttributes(Ctx,ATTRIBS, [computeAnswer=[side_effects_allow=[transform],intag=Tag]], ATTRIBS_NEW),
     withAttributes(_Ctx,ATTRIBS_NEW,
       ((findall(OutVoteMid,((member(In,[DO|IT]),computeInner(Ctx,Votes, In, OutVoteMid))),INNERDONE),
        NOUT=..[Tag,ATTRIBS,INNERDONE]))),!,
         withAttributes(Ctx,ATTRIBS,computeAnswer(Ctx,Votes,NOUT,OUT,VotesO)).

% <html:br/>
computeElement(Ctx,Votes,Htmlbr,ATTRIBS,Input,Output,VotesO):- atom(Htmlbr),atom_concat_safe('html:',Br,Htmlbr),!,
   computeElementMust(Ctx,Votes,html:Br,ATTRIBS,Input,Output,VotesO).
computeElement(Ctx,Votes,html:Br,ATTRIBS,Input,Output,VotesO):- atom(Br),
   computeElementMust(Ctx,Votes,Br,ATTRIBS,Input,Output,VotesO).

% <br/>
computeElement(Ctx,Votes,br,[],[],'<br/>',Votes):-!.
% <p/>
computeElement(Ctx,Votes,p,[],[],'<p/>',Votes):-!.

% <sr/>
computeElement(Ctx,Votes,sr,ATTRIBS,Input,Output,VotesO):- !,
   computeElementMust(Ctx,Votes,srai,ATTRIBS,[element(star,ATTRIBS,Input)],Output,VotesO).


% <srai/>s   
computeElement(_Ctx,Votes,srai,ATTRIBS,[],result([],srai=ATTRIBS),VotesO):-trace,!,VotesO is Votes * 0.8.

% <srai>s   
computeElement(Ctx,Votes,srai,ATTRIBS,Input,proof(Output,Proof),VotesO):- !,flatten([Input],Flat),  
  withAttributes(Ctx,ATTRIBS,
    debugOnFailureAimlEach((
      computeTemplate(Ctx,Votes,Flat,Flat0,VotesI),
      computeSRAI(Ctx,VotesI,Flat0,Mid,VotesM,Proof),
      computeTemplate(Ctx,VotesM,Mid,Output,VotesO)))).


computeElement(Ctx,Votes,li,Preconds,InnerXml,proof(Output,Preconds),VotesO):-!,
     precondsTrue(Ctx,Preconds),!,computeTemplate(Ctx,Votes,InnerXml,Output,VotesM),VotesO is VotesM * 1.1,!.

  precondsTrue(Ctx,PC):-lastMember(name=Name,PC,WO),lastMember(value=Value,WO,Rest),!,precondsTrue0(Ctx,[Name=Value|Rest]).
  precondsTrue(_Ctx,PC):-PC==[];var(PC),!.
  precondsTrue(Ctx,PC):-trace,precondsTrue0(Ctx,PC).

  precondsTrue0(_Ctx,PC):-PC==[];var(PC),!.
  precondsTrue0(Ctx,[NV|MORE]):-!,precondsTrue0(Ctx,MORE),!,precondsTrue0(Ctx,NV).
  precondsTrue0(Ctx,N=V):- peekNameValue(Ctx,user,N,Value,[]),!,valuesMatch(Ctx,Value,V).
  precondsTrue0(_Ctx,_NV):-trace.


computeElement(Ctx,Votes,random,_Attribs,List,AA,VotesO):-!,randomPick(List,Pick),computeAnswer(Ctx,Votes,Pick,AA,VotesO).
computeElement(Ctx,Votes,condition,_Attribs,List,AA,VotesO):-!,debugOnFailureAiml(once(( member(Pick,List),computeAnswerMaybe(Ctx,Votes,Pick,AA,VotesO)))).
computeElement(Ctx,Votes,gossip,_Attribs,Input,Output,VotesO):-!,computeAnswer(Ctx,Votes,Input,Output,VotesO).
computeElement(Ctx,Votes,think,_Attribs,Input,proof([],Hidden),VotesO):-!,computeTemplate(Ctx,Votes,Input,Hidden,VotesO).
computeElement(Ctx,Votes,GetSetBot,Attrib,InnerXml,Resp,VotesO):-member(GetSetBot,[get,set,bot]),!,computeGetSet(Ctx,Votes,GetSetBot,Attrib,InnerXml,Resp,VotesM),VotesO is VotesM * 1.1,!.
computeElement(Ctx,Votes,StarTag,Attrib,InnerXml,Resp,VotesO):- starType(StarTag,StarName),!,computeStar(Ctx,Votes,StarName,Attrib,InnerXml,Resp,VotesM),VotesO is VotesM * 1.1,!.

computeElement(Ctx,Votes,cycrandom,_Attribs,RAND,Output,VotesO):-!, computeAnswer(Ctx,Votes,cyceval(RAND),RO,VotesO),randomPick(RO,Output).

computeElement(Ctx,Votes,Tag,Attribs,Input,result(RESULT,Tag=EVAL),VotesO):- 
   member(Tag,[system]),!,
   checkNameValue(Ctx,Attribs,[lang],Lang, 'bot'),
   computeTemplate(Ctx,Votes,Input,EVAL,VotesO),
   systemCall(Ctx,Lang,EVAL,RESULT).

computeElement(Ctx,Votes,Tag,Attribs,Input,result(RESULT,Tag=EVAL),VotesO):- 
   member(Tag,[cycsystem,cyceval,cycquery]),!,
   checkNameValue(Ctx,Attribs,[lang],Lang, Tag),  
   computeTemplate(Ctx,Votes,Input,EVAL,VotesO),
   systemCall(Ctx,Lang,EVAL,RESULT).

computeElement(Ctx,Votes,Tag,ATTRIBS,Input,result(RESULT,Tag=EVAL),VotesO):- 
   member(Tag,[load,learn]),!,
   computeTemplate(Ctx,Votes,Input,EVAL,VotesO),
   tag_eval(Ctx,element(Tag,ATTRIBS,EVAL),RESULT).


computeElement(Ctx,Votes,Tag,Attrib, DOIT, result(OUT,Tag=Attrib), VotesO) :- member(Tag,[template,pre]), !, computeTemplate(Ctx,Votes,DOIT,OUT,VotesO).
computeElement(Ctx,Votes,Tag,Attrib, DOIT, result(OUT,Tag=Attrib), VotesO) :- formatterProc(Tag), !, computeTemplate(Ctx,Votes,DOIT,OUT,VotesO).

computeElement(_Ctx,Votes,Tag,Attribs,[],result([],Tag,Attribs),Votes):-!,trace.
computeElement(Ctx,Votes,Tag,Attribs,InnerXml,result(Inner,Tag=Attribs),VotesO):-!,computeTemplate(Ctx,Votes,InnerXml,Inner,VotesO).

computeElement(Ctx,Votes,Tag,Attribs,InnerXml,Resp,VotesO):- 
  GETATTRIBS = element(Tag,Attribs,InnerXml), 
  convert_element(Ctx,GETATTRIBS,GETATTRIBS0), 
  GETATTRIBS \== GETATTRIBS0,!,
  trace,
  convert_element(Ctx,GETATTRIBS,_GETATTRIBS1), 
  computeAnswer(Ctx,Votes,GETATTRIBS0, Resp,VotesO).

% ===============================================================================================
% Compute Star
% ===============================================================================================

computeStar(Ctx,Votes,Star,Attribs,InnerXml,Resp,VotesO):- 
    lastMember(index=Index,Attribs,AttribsNew),!,
    computeStar0(Ctx,Votes,Star,Index,AttribsNew,InnerXml,Resp,VotesO),!.
computeStar(Ctx,Votes,Star,Attribs,InnerXml,Resp,VotesO):-
    computeStar0(Ctx,Votes,Star,1,Attribs,InnerXml,Resp,VotesO),!.

computeStar0(Ctx,Votes,Star,Index,ATTRIBS,_InnerXml,proof(ValueO,StarVar=ValueI),VotesO):- atom_concat(Star,Index,StarVar),
      getAliceMem(Ctx,_Dict,StarVar,ValueI),!,
      computeTemplate(Ctx,Votes,element(template,ATTRIBS,ValueI),ValueO,VotesM),VotesO is VotesM * 1.1.

computeStar0(_Ctx,Votes,Star,Index,ATTRIBS,InnerXml,Resp,VotesO):-
      Resp = result(Star,Index,ATTRIBS,InnerXml),!,VotesO is Votes * 1.1.


% ===============================================================================================
% Compute Get/Set Probilities
% ===============================================================================================
/*
computeGetSet(Ctx,Votes,GetSetBot,ATTRIBS,[],Resp,VotesO):- !,computeGetSet(Ctx,Votes,GetSetBot,user,ATTRIBS,Resp,VotesO).
computeGetSet(Ctx,Votes,GetSetBot,name=NAME,MORE,Resp,VotesO):- !, computeGetSet(Ctx,Votes,GetSetBot,user,[name=NAME|MORE],Resp,VotesO).
computeGetSet(Ctx,Votes,GetSetBot,WHO,[X],Resp,VotesO):- !,computeGetSet(Ctx,Votes,GetSetBot,WHO,X,Resp,VotesO).
computeGetSet(Ctx,Votes,GetSetBot,ATTRIBS,Value,Resp,VotesO):- delete(ATTRIBS,type=bot,NEW),!,computeGetSet(Ctx,Votes,bot=GetSetBot,NEW,Value,Resp,VotesO).
computeGetSet(Ctx,Votes,GetSetBot,TYPE,[],Resp,VotesO):- !,computeGetSet(Ctx,Votes,GetSetBot,user,TYPE,Resp,VotesO).
*/
computeGetSet(Ctx,Votes,bot,ATTRIBS,InnerXml,Resp,VotesO):-!,computeGetSetVar(Ctx,Votes,bot,get,_VarName,ATTRIBS,InnerXml,Resp,VotesO).
computeGetSet(Ctx,Votes,GetSet,ATTRIBS,InnerXml,Resp,VotesO):-computeGetSetVar(Ctx,Votes,user,GetSet,_VarName,ATTRIBS,InnerXml,Resp,VotesO).


%%computeGetSetVar(Ctx,Votes,_Dict,bot,VarName,ATTRIBS,InnerXml,Resp,VotesO):- !,computeGetSetVar(Ctx,Votes,user,get,VarName,ATTRIBS,InnerXml,Resp,VotesO).
%% computeGetSetVar(Ctx,Votes,Dict,GetSetBot,VarName,ATTRIBS,InnerXml,Resp,VotesO).

computeGetSetVar(Ctx,Votes,Dict,GetSet,_OVarName,ATTRIBS,InnerXml,Resp,VotesO):- atom(ATTRIBS),ATTRIBS \= [],!, VarName = ATTRIBS,
     computeGetSetVar(Ctx,Votes,Dict,GetSet,VarName,[],InnerXml,Resp,VotesO).

computeGetSetVar(Ctx,Votes,Dict,GetSet,_OVarName,ATTRIBS,InnerXml,Resp,VotesO):-  
      member(N,[name,var]),
      lastMember(N=VarName,ATTRIBS,NEW),
      computeGetSetVar(Ctx,Votes,Dict,GetSet,NEW,VarName,InnerXml,Resp,VotesO).

computeGetSetVar(Ctx,Votes,_Dict,GetSet,VarName,ATTRIBS,InnerXml,Resp,VotesO):- 
      member(N,[type,dict,user,botname,username]),
      lastMember(N=Dict,ATTRIBS,NEW),!,
      computeGetSetVar(Ctx,Votes,Dict,GetSet,NEW,VarName,InnerXml,Resp,VotesO).

computeGetSetVar(Ctx,Votes,Dict,get,VarName,ATTRIBS,_InnerXml,proof(ValueO,VarName=ValueI),VotesO):-!,
      getAliceMem(Ctx,Dict,VarName,ValueI),!,
      computeTemplate(Ctx,Votes,element(template,ATTRIBS,ValueI),ValueO,VotesM),VotesO is VotesM * 1.1.

computeGetSetVar(Ctx,Votes,Dict,set,VarName,ATTRIBS,InnerXml,proof(ValueO,VarName=InnerXml),VotesO):-!,
      computeTemplate(Ctx,Votes,element(template,ATTRIBS,InnerXml),ValueO,VotesM),
      setAliceMem(Ctx,Dict,VarName,ValueO),!,VotesO is VotesM * 1.1.

computeGetSetVar(_Ctx,_Votes,_Get,_,_,_,_):-!,fail.

% ===============================================================================================
% Compute Answer Probilities
% ===============================================================================================

computeAnswer(_,Votes,IN,_,_):-debugFmt(computeAnswer(_,Votes,IN,_,_)),fail.

computeAnswer(Ctx,Votes,MidVote - In,Out,VotesO):- trace, !, computeAnswer(Ctx,Votes,In,Out,VotesA), VotesO is VotesA * MidVote.

computeAnswer(_Ctx,Votes,_I,_,_):-(Votes>20;Votes<0.3),!,fail.

computeAnswer(_Ctx,Votes,Res, Res,Votes):-functor(Res,result,_),!.
computeAnswer(_Ctx,Votes,Res, Res,Votes):-functor(Res,proof,_),!.

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%55
% strings (must happen before list-check)
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%55
computeAnswer(Ctx,Votes,String,Out,VotesO):-string(String),string_to_atom(String,Atom),!,computeAnswer(Ctx,Votes,Atom,Out,VotesO).
computeAnswer(_Ctx,Votes,String,Atom,Votes):-is_string(String),toCodes(String,Codes),!,from_atom_codes(Atom,Codes),!.
computeAnswer(_Ctx,Votes,'$stringCodes'(List),AA,Votes):-!,from_atom_codes(AA,List),!.

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%55
% list-check
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%55
computeAnswer(_Ctx,_Votes,['*'],_,_):- !,trace,fail.

computeAnswer(_Ctx,Votes,[],[],Votes):-!.
computeAnswer(Ctx,Votes,[A|B],OO,VotesO):- 
    atomic(A) -> 
      (trace,!,computeTemplate(Ctx,Votes,B,BB,VotesO),OO=[A|BB]) ; 
      computeTemplate(Ctx,Votes,[A|B],OO,VotesO).

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%55
% atomic 
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%55
computeAnswer(Ctx,Votes,randomsentence,Output,VotesO):-!, choose_randomsentence(X),!,computeAnswer(Ctx,Votes,X,Output,VotesO).

computeAnswer(Ctx,Votes,In,Out,Votes):-atomic(In),expandVar(Ctx,In,Out).
computeAnswer(_Ctx,Votes,Resp,Resp,Votes):-atomic(Resp),!.

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%55
% elements
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%55

% element inner reductions
computeAnswer_1(Ctx,Votes, element(Tag, ATTRIBS, [DO|IT]), OUT, VotesO) :- recursiveTag(Tag),not(DO=(_-_)),!,
     appendAttributes(Ctx,ATTRIBS, [computeAnswer=[side_effects_allow=[transform],intag=Tag]], ATTRIBS_NEW),
       withAttributes(_Ctx,ATTRIBS_NEW, findall(Each,((member(In,[DO|IT]),computeInner(Ctx,Votes, In, Each))),INNERDONE)),
       computeElement(Ctx,Votes,Tag, ATTRIBS, INNERDONE, OUT, VotesO).

computeAnswer(Ctx,Votes, element(Tag, ATTRIBS, [DO|IT]), OUT, VotesO) :- recursiveTag(Tag),not(DO=(_-_)),!,
     appendAttributes(Ctx,ATTRIBS, [computeAnswer=[side_effects_allow=[transform],intag=Tag]], ATTRIBS_NEW),
         withAttributes(Ctx,ATTRIBS_NEW, maplist_safe(computeInnerEach(Ctx, Votes),[DO|IT],INNERDONE)),
       prolog_must(ground(INNERDONE)),
       computeElementMust(Ctx,Votes,Tag, ATTRIBS, INNERDONE, OUT, VotesO).

computeAnswer(Ctx,Votes,element(Tag,Attribs,List),Out,VotesO):-computeElement(Ctx,Votes,Tag,Attribs,List,Out,VotesO),!.

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%55
% Other Compounds
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%55
computeAnswer(Ctx,Votes,GETATTRIBS, Resp,VotesO):- 
  convert_element(Ctx,GETATTRIBS,GETATTRIBS0), 
  GETATTRIBS \== GETATTRIBS0,!,
  trace,
  convert_element(Ctx,GETATTRIBS,_GETATTRIBS1),
  computeAnswer(Ctx,Votes,GETATTRIBS0, Resp,VotesO).

computeAnswer(Ctx,Votes,GETATTRIBS, Resp,VotesO):- GETATTRIBS=..[GET], isAimlTag(GET), !, computeElementMust(Ctx,Votes,GET,[],[],Resp,VotesO).
computeAnswer(Ctx,Votes,GETATTRIBS, Resp,VotesO):- GETATTRIBS=..[GET,ATTRIBS], isAimlTag(GET), !, computeElementMust(Ctx,Votes,GET,ATTRIBS,[],Resp,VotesO).
computeAnswer(Ctx,Votes,GETATTRIBS, Resp,VotesO):- GETATTRIBS=..[GET,ATTRIBS,INNER], isAimlTag(GET), !, computeElementMust(Ctx,Votes,GET,ATTRIBS,INNER,Resp,VotesO).

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%55
% errors
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%55
computeAnswer(Ctx,Votes,element(Tag,Attribs,List),Out,VotesO):-trace,computeElement(Ctx,Votes,Tag,Attribs,List,Out,VotesO),!.
computeAnswer(_Ctx,Votes,Resp,Resp,Votes):-aiml_error(computeAnswer(Resp)).


% ===============================================================================================
% Apply Input Match
% ===============================================================================================

computeSRAI(_Ctx,_Votes,[],_,_,_Proof):- !,trace, fail.
computeSRAI(Ctx,Votes,Input,TopicStarO,VotesO,Proof):-
   convertToMatchable(Input,InputO),!,
   computeSRAI0(Ctx,Votes,Input,InputO,TopicStarO,VotesO,Proof).

computeSRAI0(Ctx,Votes,Orig,Input,TopicStarO,VotesO,Proof):-
  findall(TopicStarM=VotesM:ProofM,(((computeSRAI2(Ctx,Votes,Orig,Input,TopicStarM,VotesM,ProofM)))),FOUND),
  FOUND=[_|_], !, member(TopicStarO=VotesO:Proof,FOUND),
   debugFmt(computeSRAI(Input,Proof)), TopicStarO \= [*].

computeSRAI0(Ctx,Votes,Orig,Input,TopicStarO,VotesO,fail(computeSRAI2(Orig,Input,TopicStarO,Proof))):- !,
      debugFmt(fail(computeSRAI2(Ctx,Votes,Orig,Input,TopicStarO,Votes,Proof))),!,VotesO is Votes * 0.7.
% now trace is ok

% this next line is what it does on fallback
computeSRAI0(Ctx,Votes,Orig,[B|Flat],[B|TopicStarO],VotesO,Proof):-computeSRAI2(Ctx,Votes,Orig,Flat,TopicStarO,VotesO,Proof).

computeSRAI2(Ctx,Votes,Orig,Input,Out,VotesO,Proof):-
	 getLastSaidAsInput(That),         
	 get_matchit('that',That,MatchThat,CallThat),
         get_matchit('pattern',Input,MatchInput,CallPattern),
         get_aiml_that(Ctx,MatchThat,MatchInput,Out,Proof),
         CallThat,
         CallPattern,!, VotesO is Votes * 1.1.

computeSRAI2(Ctx,Votes,Orig,Input,Out,VotesO,Proof):-
	 getLastSaidAsInput(That),         
	 get_matchit('that',That,MatchThat,CallThat),
         get_matchit('pattern',Input,MatchInput,CallPattern),
         get_aiml_that(Ctx,MatchThat,MatchInput,Out,Proof),!,
         CallThat,
         CallPattern,
       debugOnFailureAimlTrace((
	 rateMatch(MatchThat,That,Out,_NewThat,ThatVote,_ThatStar), 
	 rateMatch(MatchInput,Orig,Out,_Next,Voted,_), 
	%% flatten([Next],NextO),
	%% subst(NextO,topicstar,ThatStar,ThatStarO),
	 VotesO is Votes * (Voted + ThatVote))).

/*
set_matchit([_,Input|_],[_,Input|_]).
set_matchit([Input|_],[Input|_]).
set_matchit([_,_,Input|_],[_,Input|_]).
set_matchit([_,Input|_],[_,_,Input|_]).
%%set_matchit([Input|_],[_,Input|_]).
*/

get_matchit(StarName,Input,MatchInput,CallPattern):-
   flag(StarName,_,1),set_matchit(StarName,Input,MatchInput,CallPattern).
get_matchit(StarName,Input,MatchInput,CallPattern):-
   flag(StarName,_,1),set_matchit00(StarName,Input,MatchInput,CallPattern).

set_matchit(_StarName,Input,Input,true).
set_matchit(StarName,Input,[*],setStar(StarName,_,Input)).

set_matchit0(StarName,[H|Input],[H|Matcher],OnBind):-set_matchit1(StarName,Input,Matcher,OnBind).
set_matchit0(StarName,[H|Input],['_'|Matcher],(setStar(StarName,_,H),OnBind)):-set_matchit1(StarName,Input,Matcher,OnBind).


set_matchit00(StarName,[H|Input],['_'|Matcher],(setStar(StarName,_,H),OnBind)):-set_matchit(StarName,Input,Matcher,OnBind).
set_matchit00(StarName,[H|Input],[H|Matcher],OnBind):-set_matchit(StarName,Input,Matcher,OnBind).

setStar(StarName,N,Input):-ignore((var(N),flag(StarName,N,N))),
   atom_concat(StarName,N,StarNameN),setAliceMem(_Ctx,user,StarNameN,Input),!,flag(StarName,NN,NN+1).

set_matchit1(StarName,Input,Matcher,OnBind):- length(Input,MaxLen0), MaxLen is MaxLen0 + 2,
   set_matchit2(StarName,Input,Matcher,MaxLen,OnBind).

set_matchit2(StarName,Input,Matcher,MaxLen,(setStar(StarName,_,Left),setStar(StarName,_,Right))):-member(Mid,[[_,_,_,_],[_,_,_],[_,_],[_]]),
   sublistspan(Left,Mid,Right,Input,MaxLen),
   set_matchit3(StarName,Input,Matcher,MaxLen,Mid).

set_matchit3(_StarName,_Input,Matcher,MaxLen,Mid):-sublistspan(_Left,Mid,_Right,Matcher,MaxLen).

%%sublistspan(Mid,Full,MaxLen):-sublistspan(_Left,Mid,_Right,Full,MaxLen).
sublistspan(Left,Mid,Right,Full,MaxLen):-append(Left,MidRight,Full),append(Mid,Right,MidRight),length(Full,FLen),FLen =< MaxLen.


%get_aiml_that(Ctx,SaidThat,Match,OOut):-get_aiml_cyc(SaidThat,Match,Out),(([srai(Out)] = OOut);OOut=Out).
get_aiml_that(_Ctx,SaidThat,Match,Out,what(SaidThat, Match,Out)):-what(SaidThat, Match,Out).
get_aiml_that(_Ctx,[*],Match,Out,response(Match,Out)):-response(Match,Out).

get_aiml_that(_CTX,[T|HAT],MATCH,OUT,aimlCate(_GRAPH,_PRECALL,_TOPIC,[T|HAT],_INPUT,MATCH,_FLAGS,_CALL,_GUARD,_USERDICT,OUT,_LINENO,_SRCFILE)):-aimlCate(_GRAPH,_PRECALL,_TOPIC,[T|HAT],_INPUT,MATCH,_FLAGS,_CALL,_GUARD,_USERDICT,OUT,_LINENO,_SRCFILE).
get_aiml_that(_CTX,[*],MATCH,OUT,aimlCate(_GRAPH,_PRECALL,_TOPIC,(*),_INPUT,MATCH,_FLAGS,_CALL,_GUARD,_USERDICT,OUT,_LINENO,_SRCFILE)):-aimlCate(_GRAPH,_PRECALL,_TOPIC,(*),_INPUT,MATCH,_FLAGS,_CALL,_GUARD,_USERDICT,OUT,_LINENO,_SRCFILE).

%get_aiml_cyc([*],[String|ListO],[Obj,*]):-poStr(Obj,[String|List]),append(List,[*],ListO).
%get_aiml_cyc([*],[String,*],[Obj,*]):-poStr(Obj,String).


% ===============================================================================================
% Rate Match -  rateMatch(Markup,Original,InputBefore,InputAfter,Cost,Consumed)
% ===============================================================================================
rateMatch([],[],Out,Out,1,[]):-!.

rateMatch(Match,Match,Out,Out,1.3,[]):-!.

%%rateMatch(Match,More,Out,OOut,Vote2,Grabbed):-!,rateMatch(Match,More,Out,OOut,Vote2,Grabbed).

rateMatch([This|More],[This|More2],Out,OOut,Vote2,Grabbed):-
      rateMatch(More,More2,Out,OOut,Vote,Grabbed),
      Vote2 is Vote * (1.11).

rateMatch([*],More,Out,OOut,0.77,More):-!,subst(Out,*,More,OOut),!.
rateMatch(['_'],More,Out,OOut,0.87,More):-!,subst(Out,*,More,OOut),!.

rateMatch([*|Rest],More,Out,OOut,VoteO,Grabbed):-
      append(Grabbed,Rest,More),
      rateMatch(More,_More2,Out,OOut,Vote,_),
      subst(Out,*,Grabbed,OOut),
      VoteO is Vote * (0.72).

rateMatch(['_'|Rest],More,Out,OOut,VoteO,Grabbed):-
      append(Grabbed,Rest,More),
      rateMatch(More,_More2,Out,OOut,Vote,_),
      subst(Out,*,Grabbed,OOut),
      VoteO is Vote * (0.82).

rateMatch([This0|More],[This1|More2],Out,OOut,Vote2,Grabbed):- valuesMatch(_Ctx,This0,This1),!,
      rateMatch(More,More2,Out,OOut,Vote,Grabbed),
      Vote2 is Vote * (1.11).

rateMatch(More,More2,Out,Out2,1.0,Grabbed):-ignore(Out=Out2),ignore(Out=Grabbed),!,debugFmt(rateMatch(More,More2,Out,Out2,1.0,Grabbed)),trace,!.

% ===============================================================================================
% Run answer procs
% ===============================================================================================

choose_randomsentence(X):-
	repeat,
		retract(random_sent(Y)),
		assertz(random_sent(Y)),
		4 is random(10),!,
		Y=X.

getAliceMem(_Ctx,Dict,X,E):-dict(Dict,X,E),!.
getAliceMem(_Ctx,Dict,X,[unknown,Dict,X]):-!.
 
setAliceMem(Ctx,Dict,X,[E]):-!,setAliceMem(Ctx,Dict,X,E),!.
%%setAliceMem(_Ctx,_Dict,_X,'nick').
setAliceMem(_Ctx,Dict,X,E):- retractall(dict(Dict,X,_)),asserta(dict(Dict,X,E)),writeln(debug(dict(Dict,X,E))),!.
:-dynamic(dict/3).

% ===============================================================================================
% Get and rember Last Said
% ===============================================================================================

:-dynamic(getLastSaid/1).
getLastSaid(['where',am,'I']).

rememberSaidIt([]):-!.
rememberSaidIt(_-R1):-!,rememberSaidIt(R1).
rememberSaidIt(R1):-append(New,'.',R1),!,rememberSaidIt(New).
rememberSaidIt(R1):-ignore(retract(getLastSaid(_))),answerOutput(R1,SR1),!,asserta(getLastSaid(SR1)).

getLastSaidAsInput(LastSaid):-getLastSaid(That),convertToMatchable(That,LastSaid),!.


% ===============================================================================================
% Template Output to text
% ===============================================================================================

answerOutput(Output,NonVar):-nonvar(NonVar),answerOutput(Output,Var),!,valuesMatch(_Ctx,MatchVar,NonVar).
answerOutput(Output,[Output]):-var(Output),!.
answerOutput([],Output):- !, Output=[].
answerOutput(Output,Split):-atom(Output),atomSplit(Output,Split),!.
answerOutput(Output,[Output]):-atomic(Output),!.
answerOutput([A|AA],Output):-!,
   answerOutput(A,B),
   answerOutput(AA,BB),
   flatten([B,BB],Output).
answerOutput(element(Tag,Attribs,InnerXML),[element(Tag,Attribs,Output)]):-trace,answerOutput(InnerXML,Output).
answerOutput(Term,Output):-compound(Term),arg(1,Term,Mid),!,answerOutput(Mid,Output).
answerOutput(Output,[Output]):-!.

% ===============================================================================================
% Convert to Matchable
% ===============================================================================================

convertToMatchable(That,LastSaid):-
      answerOutput(That,AA),!,
      deleteAll(AA,['.','!','?','\'','!','','\n'],Words),
      toUppercase(Words,LastSaid),!.

deleteAll(A,[],A):-!.
deleteAll(A,[L|List],AA):-delete(A,L,AAA),deleteAll(AAA,List,AA),!.
% ===============================================================================================
% Degrade Response
% ===============================================================================================

:-dynamic(degraded/1).

degrade(_-OR):-!,degrade(OR).
degrade(OR):-asserta(degraded(OR)).
   
:- exists_file('logicmoo_module_aiml_eval.pl')-> cd('..') ; true.



aimlDebugFmt(X):-debugFmt(X),!.

:-ensure_loaded('cynd/logicmoo_module_aiml_shared.pl').
%% :-ensure_loaded('cynd/logicmoo_module_aiml_shared.pl').
:-ensure_loaded('cynd/logicmoo_module_aiml_xpath.pl').

:-traceAll.

:-ensure_loaded('cynd/logicmoo_module_aiml_loader.pl').
:-ensure_loaded('cynd/logicmoo_module_aiml_eval.pl').


%:-ensure_loaded('bootstrap.aiml.pl').

%:-load_aiml_files.

%:-debug,run_chat_tests.

%:-main_loop.

% :- tell(listing1),listing,told.

%:- guitracer.

dtt:- time(dt),statistics,alicebot.

dttt:-time(consult(aimlCate_checkpoint)),alicebot.

:-list_undefined.
%:-dttt.

:-do.

