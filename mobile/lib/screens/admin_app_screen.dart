import 'package:flutter/material.dart';

class AdminAppScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Admin Control Panel'), backgroundColor: Colors.blueGrey),
      body: Padding(
        padding: EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Overview', style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold)),
            SizedBox(height: 10),
            Row(
              children: [
                _buildKpiCard('Candidates', '1,200'),
                _buildKpiCard('Payments', '₹50,000'),
                _buildKpiCard('Marriages', '45'),
              ],
            ),
            SizedBox(height: 20),
            Text('Recent Proposals', style: TextStyle(fontSize: 20)),
            Expanded(
              child: ListView.builder(
                itemCount: 5,
                itemBuilder: (context, index) {
                  return ListTile(
                    title: Text('Proposal #${index + 101}'),
                    subtitle: Text('Groom A -> Bride B'),
                    trailing: Text('Pending'),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildKpiCard(String label, String value) {
    return Expanded(
      child: Card(
        child: Padding(
          padding: EdgeInsets.all(16.0),
          child: Column(
            children: [
              Text(label, style: TextStyle(fontSize: 14)),
              Text(value, style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
            ],
          ),
        ),
      ),
    );
  }
}
