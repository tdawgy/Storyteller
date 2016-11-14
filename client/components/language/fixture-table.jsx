import React from 'react';
import _ from 'lodash';
import { connect } from 'react-redux';
const Specification = require('../../lib/model/specification');
import { preview, editing } from './../editing/component-loader';
import { Grid, Row, Col, Tabs, Tab } from 'react-bootstrap';
var Postal = require('postal');


function toErrorMessage(data){
	if (data.error != null && data.message != null){
		return `${data.message} -> ${data.error}`;
	}

	return data.message || data.error;
}


function FixtureError({error}){
    const text = toErrorMessage(error);

    return (
        <pre>{text}</pre>
    );
}

function GrammarError({error, grammar}){
    const text = toErrorMessage(error);

    let title = null;
    if (grammar != null && grammar != 'null'){
        title = (<h4>Grammar: {grammar}</h4>);
    }

    return (
        <div>
            {title}
            <pre>
                {text}
            </pre>
        </div>
    );
}


function FixtureTable({fixture, spec}){
  const previews = spec.previews(preview);
  const editors = spec.editors(editing);

  var errorTab = null;

  if (fixture.errorCount() > 0){
    var errors = fixture.errors.map(err => FixtureError({error: err}));

		for (var key in fixture.grammars){
			var g = fixture.grammars[key];
      if (g.errors){
        for (var i = 0; i < g.errors.length; i++){
          var errorElement = (<GrammarError error={g.errors[i]} grammar={g}/>);
          errors.push(errorElement);
        }
      }
		}

    errorTab = (<Tab eventKey={3} title="Errors">{errors}</Tab>)
  }

  var exportMsg = {type: 'open-fixture-file', key: fixture.key, export: true};
  var exportAndEdit = () => Postal.publish({channel: 'engine-request', topic: 'open-fixture-file', data: exportMsg});

  return (
    <div>
      <h3><a onClick={exportAndEdit} title="Click to export and edit the markdown specification for this fixture">{fixture.title}</a> ({fixture.implementation})</h3>
      <hr />
      <Grid>
        <Row>
          <p>Below is a sample specification for this fixture just to preview its usage</p>
          <Tabs defaultActiveKey={1} id="uncontrolled-tab-example">
            <Tab eventKey={1} title="Preview">{previews}</Tab>
            <Tab eventKey={2} title="Editing">{editors}</Tab>
            {errorTab}
          </Tabs>
        </Row>
      </Grid>
    </div>
  );
}

function getFixture(state, ownProps){
  var fixtures = state.get('fixtures');
  var fixture = fixtures.find(ownProps.params.key);

  var spec = new Specification(fixture.sample, fixtures);

  return {fixture: fixture, spec: spec};
}

module.exports = connect(getFixture)(FixtureTable);
