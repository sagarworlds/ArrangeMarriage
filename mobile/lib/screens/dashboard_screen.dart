import 'package:flutter/material.dart';
import 'match_screen.dart';
import 'profile_screen.dart';

class DashboardScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Member Dashboard')),
      body: GridView.count(
        crossAxisCount: 2,
        padding: EdgeInsets.all(16),
        children: [
          _buildMenuCard(context, 'Find Matches', Icons.search, MatchScreen()),
          _buildMenuCard(context, 'My Profile', Icons.person, ProfileScreen()),
          _buildMenuCard(context, 'Proposals', Icons.mail, null),
          _buildMenuCard(context, 'Payments', Icons.payment, null),
        ],
      ),
    );
  }

  Widget _buildMenuCard(BuildContext context, String title, IconData icon, Widget? screen) {
    return Card(
      child: InkWell(
        onTap: () {
          if (screen != null) {
            Navigator.push(context, MaterialPageRoute(builder: (context) => screen));
          }
        },
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 50, color: Colors.red),
            SizedBox(height: 10),
            Text(title, style: TextStyle(fontWeight: FontWeight.bold)),
          ],
        ),
      ),
    );
  }
}
