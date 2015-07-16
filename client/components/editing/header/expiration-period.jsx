/** @jsx React.DOM */
var React = require("react");
var Postal = require('postal');
var changes = require('./../../../lib/model/change-commands');
var range = require('./../../../lib/array-helpers').range;
var hierarchy = require('./../../../lib/stores/hierarchy');

var ExpirationPeriod = React.createClass({
  changeFunc(e){
    const value = e.target.value;
    const number = parseInt(value, 10);
    e.preventDefault();
    if (isNaN(number)) {
      return;
    }

    Postal.publish({
      channel: 'editor',
      topic: 'changes',
      data: changes.changeExpirationPeriod(number)
    });
  },

  clickFunc(e) {
    e.preventDefault();
    hierarchy.bumpSpecDate(this.props.spec);
  },

  getExpirationPeriod(){
    return this.props.spec['expiration-period']
  },

  getSelect(){
    const options = range(0, 12).map(function (val) {
      return <option value={val} key={val}>{val}</option>
    })
    return <select id="expiration-period-select" onChange={this.changeFunc} type="text" value={this.getExpirationPeriod()}>{options}</select>;
  },

	render(){
    var message = this.getExpirationPeriod() ? <p>Expires in: {this.getSelect()} months.</p> : <p>Never expires. {this.getSelect()} months.</p>;
    return <div id='expiration-period' className='clearfix'>
      {message}
      <p><em><small>Last Updated: {this.props.spec['last-updated']}</small></em></p>
      <button disabled={this.props.disabled} className='pull-right btn' onClick={this.clickFunc}>Update</button>
    </div>;
  },
});

module.exports = ExpirationPeriod;
